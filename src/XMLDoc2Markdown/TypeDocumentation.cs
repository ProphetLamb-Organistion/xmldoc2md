using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Dawn;

using Markdown;

using XMLDoc2Markdown.Extensions;

namespace XMLDoc2Markdown
{
    internal class TypeDocumentation
    {
        private readonly Assembly assembly;
        private readonly XmlDocumentation documentation;
        private readonly TypeSymbol symbol;
        private IMarkdownDocument document = new MarkdownDocument();

        public TypeDocumentation(Assembly assembly, TypeSymbol symbol, XmlDocumentation documentation)
        {
            this.assembly = assembly;
            this.symbol = symbol;
            this.documentation = documentation;
        }

        public override string ToString()
        {
            Type type = this.symbol.SymbolType;
            this.document.AppendHeader(this.symbol.SimplifiedName.FormatChevrons(), 1);

            this.document.AppendParagraph($"Namespace: {type.Namespace}");

            XElement? typeDocElement = this.documentation.GetMember(type);

            this.WriteMemberInfoSummary(typeDocElement);
            this.WriteMemberInfoSignature(type);
            this.WriteTypeParameters(type, typeDocElement);

            if (type.BaseType != null)
            {
                this.document.AppendParagraph($"Inheritance {string.Join(" → ", this.symbol.GetInheritanceHierarchy().Reverse().Select(t => t.GetDocsLink(this.symbol)))}");
            }

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                this.document.AppendParagraph($"Implements {string.Join(", ", interfaces.Select(i => i.ToSymbol().GetDocsLink(this.symbol)))}");
            }

            this.WriteMembersDocumentation(type.GetProperties());
            this.WriteMembersDocumentation(type.GetConstructors());
            this.WriteMembersDocumentation(
                type
                   .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                   .Where(m => !m.IsSpecialName)
                   .Where(m => !m.IsPrivate));
            this.WriteMembersDocumentation(type.GetEvents());

            if (type.IsEnum)
            {
                this.WriteEnumFields(type.GetFields().Where(m => !m.IsSpecialName));
            }

            this.document.AppendParagraph(new MarkdownLink("`< Index`", TypeSymbolProvider.Instance["index"].GetInternalDocsUrl(this.symbol)));

            return this.document.ToString();
        }

        private void WriteMemberInfoSummary(XElement? memberDocElement)
        {
            string summary = string.Empty;
            IEnumerable<XNode>? nodes = memberDocElement?.Element("summary")?.Nodes();
            if (nodes != null)
            {
                summary = nodes.Aggregate(
                    summary,
                    (current, node) => current
                      + node
                            switch
                            {
                                XText text => text,
                                XElement element => this.PrintSummaryXElement(element),
                                _ => null
                            });
            }

            if (!string.IsNullOrWhiteSpace(summary))
            {
                this.document.AppendParagraph(summary);
            }
        }

        private string? PrintSummaryXElement(XElement element) =>
            element.Name.ToString() switch
            {
                "see" => this.GetLinkFromReference(element.Attribute("cref")?.Value).ToString(),
                _ => element.Value
            };

        private void WriteMemberInfoSignature(MemberInfo memberInfo) =>
            this.document.AppendCode(
                "csharp",
                memberInfo.GetSignature(true));

        private void WriteMembersDocumentation(IEnumerable<MemberInfo> members)
        {
            Guard.Argument(members, nameof(members)).NotNull();

            members = members.Where(member => member != null);

            if (members.Count() <= 0)
            {
                return;
            }

            MemberTypes memberType = members.First().MemberType;
            string title = memberType switch
            {
                MemberTypes.Property => "Properties",
                MemberTypes.Constructor => "Constructors",
                MemberTypes.Method => "Methods",
                MemberTypes.Event => "Events",
                MemberTypes.Field => "Fields",
                _ => throw new NotImplementedException()
            };
            this.document.AppendHeader(title, 2);

            foreach (MemberInfo member in members)
            {
                this.document.AppendHeader(member.GetSignature().FormatChevrons(), 3);

                XElement? memberDocElement = this.documentation.GetMember(member);

                this.WriteMemberInfoSummary(memberDocElement);
                this.WriteMemberInfoSignature(member);

                switch (member)
                {
                    case MethodBase methodBase:
                    {
                        IMarkdownDocument doc = this.document;
                        this.document = this.document.CreateBlockquoteDocument();
                        this.WriteTypeParameters(methodBase, memberDocElement);
                        this.WriteMethodParams(methodBase, memberDocElement);

                        if (methodBase is MethodInfo methodInfo && methodInfo.ReturnType != typeof(void))
                        {
                            this.WriteMethodReturnType(methodInfo, memberDocElement);
                        }

                        this.document = doc;
                        break;
                    }
                    case PropertyInfo propertyInfo:
                    {
                        IMarkdownDocument doc = this.document;
                        this.document = this.document.CreateBlockquoteDocument();
                        this.document.AppendHeader("Property Value", 4);

                        string? valueDoc = memberDocElement?.Element("value")?.Value;
                        this.document.AppendParagraph($"{new MarkdownInlineCode(propertyInfo.GetReturnType()?.ToSymbol().DisplayName)}<br>{valueDoc}");
                        this.document = doc;
                        break;
                    }
                }

                this.WriteExceptions(memberDocElement);
            }

            this.document.AppendHorizontalRule();
        }

        private void WriteExceptions(XElement? memberDocElement)
        {
            IEnumerable<XElement>? exceptionDocs = memberDocElement?.Elements("exception");

            if (exceptionDocs is null || exceptionDocs.Count() <= 0)
            {
                return;
            }

            this.document.AppendHeader("Exceptions", 4);

            foreach (XElement exceptionDoc in exceptionDocs)
            {
                var text = new List<string>(2);

                string? cref = exceptionDoc.Attribute("cref")?.Value;
                if (cref != null && cref.Length > 2)
                {
                    int index = cref.LastIndexOf('.');
                    string exception = cref.Substring(index + 1);
                    if (!string.IsNullOrEmpty(exception))
                    {
                        text.Add(exception);
                    }
                }

                if (!string.IsNullOrEmpty(exceptionDoc.Value))
                {
                    text.Add(exceptionDoc.Value);
                }

                if (text.Count() > 0)
                {
                    this.document.AppendParagraph(string.Join("<br>", text));
                }
            }
        }

        private void WriteMethodReturnType(MethodInfo methodInfo, XElement? memberDocElement)
        {
            Guard.Argument(methodInfo, nameof(methodInfo)).NotNull();

            this.document.AppendHeader("Returns", 4);

            string? returnsDoc = memberDocElement?.Element("returns")?.Value;
            this.document.AppendParagraph($"{methodInfo.ReturnType.ToSymbol().DisplayName}<br>{returnsDoc}");
        }

        private void WriteTypeParameters(MemberInfo memberInfo, XElement? memberDocElement)
        {
            Guard.Argument(memberInfo, nameof(memberInfo)).NotNull();

            Type[] typeParams = memberInfo switch
            {
                TypeInfo typeInfo => typeInfo.GenericTypeParameters,
                MethodInfo methodInfo => methodInfo.GetGenericArguments(),
                _ => Array.Empty<Type>()
            };

            if (typeParams.Length <= 0)
            {
                return;
            }

            this.document.AppendHeader("Type Parameters", 4);

            foreach (Type typeParam in typeParams)
            {
                string? typeParamDoc = memberDocElement?.Elements("typeparam").FirstOrDefault(e => e.Attribute("name")?.Value == typeParam.Name)?.Value;
                this.document.AppendParagraph($"{new MarkdownInlineCode(typeParam.ToSymbol().DisplayName)}<br>{typeParamDoc}");
            }
        }

        private void WriteMethodParams(MethodBase methodBase, XElement? memberDocElement)
        {
            Guard.Argument(methodBase, nameof(methodBase)).NotNull();

            ParameterInfo[] @params = methodBase.GetParameters();

            if (@params.Length <= 0)
            {
                return;
            }

            this.document.AppendHeader("Parameters", 4);

            foreach (ParameterInfo param in @params)
            {
                string? paramDoc = memberDocElement?.Elements("param").FirstOrDefault(e => e.Attribute("name")?.Value == param.Name)?.Value;
                this.document.AppendParagraph($"{param.Name} : {new MarkdownInlineCode(param.ParameterType.ToSymbol().SimplifiedName)}<br>{paramDoc}");
            }
        }

        private void WriteEnumFields(IEnumerable<FieldInfo> fields)
        {
            Guard.Argument(fields, nameof(fields)).NotNull();

            if (fields.Count() > 0)
            {
                this.document.AppendHeader("Fields", 2);

                var header = new MarkdownTableHeader(
                    new MarkdownTableHeaderCell("Name"),
                    new MarkdownTableHeaderCell("Description"));

                var table = new MarkdownTable(header, fields.Count());

                foreach (FieldInfo field in fields)
                {
                    string? paramDoc = this.documentation.GetMember(field)?.Element("summary")?.Value;
                    if (paramDoc != null)
                    {
                        table.AddRow(new MarkdownTableRow(field.Name, paramDoc.Trim()));
                    }
                }

                this.document.Append(table);
            }
        }

        private MarkdownInlineElement GetLinkFromReference(string? crefAttribute)
        {
            if (crefAttribute is null || crefAttribute.Length < 2)
            {
                return new MarkdownInlineCode(string.Empty);
            }

            if (crefAttribute[1] != ':' || !MemberTypesAliases.TryGetMemberType(crefAttribute[0], out MemberTypes memberType))
            {
                return new MarkdownInlineCode(crefAttribute);
            }

            string memberFullName = crefAttribute.Substring(2);

            if (memberType == MemberTypes.TypeInfo)
            {
                Type? type = Type.GetType(memberFullName) ?? this.assembly.GetType(memberFullName);
                if (type != null)
                {
                    return type.ToSymbol().GetDocsLink(this.symbol);
                }
            }

            return new MarkdownInlineCode(memberFullName);
        }
    }
}
