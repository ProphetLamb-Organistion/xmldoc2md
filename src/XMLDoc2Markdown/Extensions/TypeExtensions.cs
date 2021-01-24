using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Markdown;

namespace XMLDoc2Markdown.Extensions
{
    internal static class TypeExtensions
    {
        internal static readonly IReadOnlyDictionary<Type, string> simplifiedTypeNames = new Dictionary<Type, string>
        {
            // void
            {typeof(void), "void"},
            // object
            {typeof(object), "object"},
            // bool
            {typeof(bool), "bool"},
            // numeric
            {typeof(sbyte), "sbyte"},
            {typeof(byte), "byte"},
            {typeof(short), "short"},
            {typeof(ushort), "ushort"},
            {typeof(int), "int"},
            {typeof(uint), "uint"},
            {typeof(long), "long"},
            {typeof(ulong), "ulong"},
            {typeof(float), "float"},
            {typeof(double), "double"},
            {typeof(decimal), "decimal"},
            // text
            {typeof(char), "char"},
            {typeof(string), "string"}
        };

        [Obsolete]
        internal static string GetSimplifiedName(this Type type) => simplifiedTypeNames.TryGetValue(type, out string? simplifiedName) ? simplifiedName : type.Name;

        [Obsolete]
        internal static Visibility GetVisibility(this Type type)
        {
            if (type.IsPublic)
            {
                return Visibility.Public;
            }

            return Visibility.None;
        }

        [Obsolete]
        internal static string GetSignature(this Type type, bool full = false)
        {
            var signature = new List<string>();

            if (full)
            {
                signature.Add(type.GetVisibility().Print());

                if (type.IsClass)
                {
                    if (type.IsAbstract && type.IsSealed)
                    {
                        signature.Add("static");
                    }
                    else if (type.IsAbstract)
                    {
                        signature.Add("abstract");
                    }
                    else if (type.IsSealed)
                    {
                        signature.Add("sealed");
                    }

                    signature.Add("class");
                }
                else if (type.IsInterface)
                {
                    signature.Add("interface");
                }
                else if (type.IsEnum)
                {
                    signature.Add("enum");
                }
                else if (type.IsValueType)
                {
                    signature.Add("struct");
                }
            }

            signature.Add(type.GetDisplayName());

            if (type.IsClass || type.IsInterface)
            {
                var baseTypeAndInterfaces = new List<Type>();

                if (type.IsClass && type.BaseType != null && type.BaseType != typeof(object))
                {
                    baseTypeAndInterfaces.Add(type.BaseType);
                }

                baseTypeAndInterfaces.AddRange(type.GetInterfaces());

                if (baseTypeAndInterfaces.Count > 0)
                {
                    signature.Add($": {string.Join(", ", baseTypeAndInterfaces.Select(t => t.Namespace != type.Namespace ? t.Namespace : String.Empty + t.GetDisplayName()))}");
                }
            }

            return string.Join(' ', signature);
        }

        [Obsolete]
        internal static string GetDisplayName(this Type type)
        {
            /*
             * Convert generic type parameters from gravis notation to type notation.
             *
             * For classes:
             *   Nested type inherit the generic type parameters of their parents. But these are not reflected in the Name property, only in FullName.
             *   In these scenarios some declaring types (parentage) are necessary for the DisplayName.
             */

            TypeInfo typeInfo = type.GetTypeInfo();
            StringBuilder sb = new StringBuilder(255);
            List<Type> genericTypeSpecifiers = new List<Type>(typeInfo.GenericTypeParameters.Length + typeInfo.GenericTypeArguments.Length);
            genericTypeSpecifiers.AddRange(typeInfo.GenericTypeParameters);
            genericTypeSpecifiers.AddRange(typeInfo.GenericTypeArguments);

            if ((typeInfo.IsClass || typeInfo.IsValueType) && typeInfo.IsNested)
            {
                // Remove generic type parameters already defined in declaring types.
                TypeInfo? parentInfo = typeInfo.DeclaringType?.GetTypeInfo();
                while (!(parentInfo is null) && parentInfo.IsGenericType)
                {
                    foreach (Type p in parentInfo.GenericTypeParameters)
                    {
                        genericTypeSpecifiers.RemoveAll(x => String.Equals(x.Name, p.Name, StringComparison.Ordinal));
                    }

                    parentInfo = parentInfo.DeclaringType?.GetTypeInfo();
                }

                // If not all generic type parameters are declared in the type, recursively append GetDisplayName of the declaring type, until all generic type parameters are covered.
                if (typeInfo.GenericTypeParameters.Length + typeInfo.GenericTypeArguments.Length != genericTypeSpecifiers.Count)
                {
                    sb.Append(typeInfo.DeclaringType?.ToSymbol().DisplayName ?? "???")
                       .Append(".");
                }
            }

            // Get the base name of the type.
            int gravisIndex = type.Name.IndexOf('`'); // Indicates beginning of the generic type parameter portion of the name.
            sb.Append(gravisIndex == -1 ? type.Name : type.Name.Substring(0, gravisIndex));

            if (genericTypeSpecifiers.Count != 0)
            {
                sb.Append('<');
                sb.AppendJoin(", ", genericTypeSpecifiers.Select(t => t.GetDisplayName()));
                sb.Append('>');
            }

            return sb.ToString();
        }

        [Obsolete]
        internal static IEnumerable<Type> GetInheritanceHierarchy(this Type type)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                yield return current;
            }
        }

        [Obsolete]
        internal static string GetMSDocsUrl(this Type type, string msdocsBaseUrl = "https://docs.microsoft.com/en-us/")
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "Type cannot be null.");
            }

            if (type.Assembly != typeof(string).Assembly)
            {
                throw new InvalidOperationException($"'{type.FullName}' is not a framework type.");
            }

            return (type.FullName is null, type.Namespace is null) switch
            {
                (true, true) => // Navigate to search specifying that we are looking for API
                $"{msdocsBaseUrl}search/?terms=api%20{type.Name}&category=Reference&scope=.NET",
                (true, false) => // Try to reconstruct from namespace, not guaranteed to hit.
                $"{msdocsBaseUrl}dotnet/api/{type.Namespace!.ToLower().Replace('`', '-')}.{type.Name.ToLower().Replace('`', '-')}",
                _ => // Navigate to documentation
                $"{msdocsBaseUrl}dotnet/api/{type.FullName!.ToLower().Replace('`', '-')}"
            };
        }

        [Obsolete]
        internal static string GetInternalDocsUrl(this Type type, string rootUrl = "..")
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "Type cannot be null.");
            }

            return $"{rootUrl}/{type.Namespace}/{type.Name.Replace('`', '-')}.md";
        }

        [Obsolete]
        internal static MarkdownLink GetDocsLink(this Type type)
        {
            string url = type.Assembly == typeof(string).Assembly
                ? type.GetMSDocsUrl()
                : type.GetInternalDocsUrl();

            return new MarkdownLink(type.GetDisplayName().FormatChevrons(), url);
        }

        public static TypeSymbol ToSymbol(this Type type) => TypeSymbolResolver.Instance.TryGetValue(type.GetTypeSymbolIdentifier(), out TypeSymbol? symbol) ? symbol : new TypeSymbol(type.GetTypeInfo());

        public static string GetTypeSymbolIdentifier(this Type type) => String.Concat(type.Namespace, ".", type.Name.Replace('`', '-'));
    }
}
