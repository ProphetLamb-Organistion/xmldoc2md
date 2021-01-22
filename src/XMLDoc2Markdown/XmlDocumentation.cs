using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using XMLDoc2Markdown.Extensions;

namespace XMLDoc2Markdown
{
    public class XmlDocumentation
    {
        public XmlDocumentation(string dllPath)
        {
            this.XmlFilePath = Path.Combine(Directory.GetParent(dllPath).FullName, Path.GetFileNameWithoutExtension(dllPath) + ".xml");

            if (!File.Exists(this.XmlFilePath))
            {
                throw new Exception($"No XML documentation file founded for library '{dllPath}'.");
            }
        }

        public string XmlFilePath { get; protected set; }

        public bool IsLoaded { get; protected set; }

        public string? AssemblyName { get; protected set; }

        public IEnumerable<XElement>? Members { get; protected set; }


        protected virtual void LoadFile()
        {
            if (this.IsLoaded)
            {
                throw new InvalidOperationException("XmlDocumentation already loaded.");
            }

            try
            {
                var xDocument = XDocument.Parse(File.ReadAllText(this.XmlFilePath));

                this.AssemblyName = xDocument.Descendants("assembly").First().Elements("name").First().Value;
                this.Members = xDocument.Descendants("members").First().Elements("member");
            }
            catch (Exception e)
            {
                throw new Exception("Unable to parse XML documentation", e);
            }

            this.IsLoaded = true;
        }

        public XmlDocumentation Load()
        {
            this.LoadFile();
            return this;
        }

        public virtual XElement? GetMember(MemberInfo memberInfo)
        {
            if (!this.IsLoaded)
            {
                throw new InvalidOperationException("XmlDocumentation is not loaded.");
            }

            string fullName;
            if (memberInfo is Type type)
            {
                fullName = type.FullName ?? type.Namespace + "." + type.Name;
            }
            else
            {
                Type declaringType = memberInfo.DeclaringType ?? throw new InvalidOperationException("DeclaringType is null.");
                string name = memberInfo is ConstructorInfo ? "#ctor" : memberInfo.Name;
                fullName = $"{declaringType.Namespace}.{declaringType.Name}.{name}";
            }

            if (memberInfo is MethodBase methodBase)
            {
                Type[] genericArguments = methodBase switch
                    {
                        ConstructorInfo _ => methodBase.DeclaringType?.GetGenericArguments(),
                        MethodInfo _ => methodBase.GetGenericArguments(),
                        _ => null
                    }
                 ?? Array.Empty<Type>();

                if (methodBase is MethodInfo && methodBase.IsGenericMethod)
                {
                    fullName += $"``{genericArguments.Length}";
                }

                ParameterInfo[] parameterInfos = methodBase.GetParameters();
                if (parameterInfos.Length > 0)
                {
                    IEnumerable<string> @params = parameterInfos.Select(
                        p =>
                        {
                            int index = Array.IndexOf(genericArguments, p.ParameterType);
                            return index > -1 ? $"{(methodBase is MethodInfo ? "``" : "`")}{index}" : p.ParameterType.ToString();
                        });
                    fullName = string.Concat(fullName, $"({string.Join(',', @params)})");
                }
            }

            return this.GetMember($"{memberInfo.MemberType.GetAlias()}:{fullName}");
        }

        public virtual XElement? GetMember(string name)
        {
            if (!this.IsLoaded)
            {
                throw new InvalidOperationException("XmlDocumentation is not loaded.");
            }
            return this.Members!.FirstOrDefault(member => member.Attribute("name")?.Value == name);
        }
    }
}
