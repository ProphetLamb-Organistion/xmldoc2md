using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Markdown;

namespace XMLDoc2Markdown
{
    [DebuggerDisplay("{" + nameof(TypeSymbol.SymbolType) + "}")]
    internal class TypeSymbol
    {
#region Fields

        private TypeInfo? _symbolTypeInfo;
        private string? _simplifiedName;
        private string? _displayName;
        private string? _signaturePostfix;

        internal static readonly IReadOnlyDictionary<Type, string> SimplifiedTypeNames = new Dictionary<Type, string>
        {
            // void
            { typeof(void), "void" },
            // object
            { typeof(object), "object" },
            // bool
            { typeof(bool), "bool" },
            // numeric
            { typeof(sbyte), "sbyte" },
            { typeof(byte), "byte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            // text
            { typeof(char), "char" },
            { typeof(string), "string" },
        };

        private const string s_virtualDeviceRoot = "A:\\";

#endregion

#region Constructor

        public TypeSymbol(Type type, string filePath, string fileNameWithoutExtension)
        {
            this.SymbolType = type;
            this.FilePath = filePath;
            this.FileNameWithoutExtension = fileNameWithoutExtension;
            this.IsWellDefined = true;
        }

        public TypeSymbol(string filePath, string fileNameWithoutExtension)
        {
            this.SymbolType = null!;
            this.FilePath = filePath;
            this.FileNameWithoutExtension = fileNameWithoutExtension;
            this.IsWellDefined = false;
        }

        public TypeSymbol(Type type)
        {
            this.SymbolType = type;
            this.FilePath = null!;
            this.FileNameWithoutExtension = null!;
            this.IsWellDefined = false;
        }

#endregion

#region Properties

        public bool IsWellDefined { get; }

        public Type SymbolType { get; }

        public TypeInfo SymbolTypeInfo => this._symbolTypeInfo ??= this.SymbolType?.GetTypeInfo() ?? throw new InvalidOperationException("SymbolType is null.");

        public string SimplifiedName
        {
            get
            {
                if (this.SymbolType is null)
                {
                    throw new InvalidOperationException("SymbolType is null.");
                }

                return this._simplifiedName ??= SimplifiedTypeNames.TryGetValue(this.SymbolType, out string? simplifiedName) ? simplifiedName : this.SymbolType.Name;
            }
        }

        public Visibility Visibility
        {
            get
            {
                if (this.SymbolType is null)
                {
                    throw new InvalidOperationException("SymbolType is null.");
                }
                return this.SymbolType.IsPublic ? Visibility.Public : Visibility.None;
            }
        }

        public string FilePath { get; }

        public string FileNameWithoutExtension { get; }

        public string FullFilePathAndName => Path.Combine(this.FilePath, this.FileNameWithoutExtension + ".md");

#endregion

#region Public members

        public string GetSignature(bool full = false)
        {
            if (this.SymbolType is null)
            {
                throw new InvalidOperationException("SymbolType is null.");
            }
            var signature = new List<string>();

            if (full)
            {
                signature.Add(this.Visibility.Print());

                if (this.SymbolType.IsClass)
                {
                    if (this.SymbolType.IsAbstract && this.SymbolType.IsSealed)
                    {
                        signature.Add("static");
                    }
                    else if (this.SymbolType.IsAbstract)
                    {
                        signature.Add("abstract");
                    }
                    else if (this.SymbolType.IsSealed)
                    {
                        signature.Add("sealed");
                    }

                    signature.Add("class");
                }
                else if (this.SymbolType.IsInterface)
                {
                    signature.Add("interface");
                }
                else if (this.SymbolType.IsEnum)
                {
                    signature.Add("enum");
                }
                else if (this.SymbolType.IsValueType)
                {
                    signature.Add("struct");
                }
            }

            signature.Add(this.DisplayName);

            if (!(this._signaturePostfix is null))
            {
                signature.Add(this._signaturePostfix);
            }
            else if (this.SymbolType.IsClass || this.SymbolType.IsInterface)
            {
                var baseTypeAndInterfaces = new List<Type>();

                if (this.SymbolType.IsClass && this.SymbolType.BaseType != null && this.SymbolType.BaseType != typeof(object))
                {
                    baseTypeAndInterfaces.Add(this.SymbolType.BaseType);
                }

                baseTypeAndInterfaces.AddRange(this.SymbolType.GetInterfaces());

                if (baseTypeAndInterfaces.Count > 0)
                {
                    this._signaturePostfix = $": {string.Join(", ", baseTypeAndInterfaces.Select(t => (t.Namespace != this.SymbolType.Namespace ? t.Namespace + "." : String.Empty) + t.ToSymbol().DisplayName))}";
                    signature.Add(this._signaturePostfix);
                }
            }

            return string.Join(' ', signature);
        }

        public string DisplayName
        {
            get
            {
                if (this.SymbolType is null)
                {
                    throw new InvalidOperationException("SymbolType is null.");
                }

                if (!(this._displayName is null))
                {
                    return this._displayName;
                }

                /*
                 * Convert generic type parameters from gravis notation to type notation.
                 *
                 * For classes:
                 *   Nested type inherit the generic type parameters of their parents. But these are not reflected in the Name property, only in FullName.
                 *   In these scenarios some declaring types (parentage) are necessary for the DisplayName.
                 */

                StringBuilder sb = new StringBuilder(255);
                List<Type> genericTypeSpecifiers = new List<Type>(this.SymbolTypeInfo.GenericTypeParameters.Length + this.SymbolTypeInfo.GenericTypeArguments.Length);
                genericTypeSpecifiers.AddRange(this.SymbolTypeInfo.GenericTypeParameters);
                genericTypeSpecifiers.AddRange(this.SymbolTypeInfo.GenericTypeArguments);

                if ((this.SymbolTypeInfo.IsClass || this.SymbolTypeInfo.IsValueType) && this.SymbolTypeInfo.IsNested)
                {
                    // Remove generic type parameters already defined in declaring types.
                    TypeInfo? parentInfo = this.SymbolTypeInfo.DeclaringType?.GetTypeInfo();
                    while (!(parentInfo is null) && parentInfo.IsGenericType)
                    {
                        foreach (Type p in parentInfo.GenericTypeParameters)
                        {
                            genericTypeSpecifiers.RemoveAll(x => String.Equals(x.Name, p.Name, StringComparison.Ordinal));
                        }
                        parentInfo = parentInfo.DeclaringType?.GetTypeInfo();
                    }
                
                    // If not all generic type parameters are declared in the type, recursively append GetDisplayName of the declaring type, until all generic type parameters are covered.
                    if (this.SymbolTypeInfo.GenericTypeParameters.Length + this.SymbolTypeInfo.GenericTypeArguments.Length != genericTypeSpecifiers.Count)
                    {
                        sb.Append(this.SymbolTypeInfo.DeclaringType?.ToSymbol().DisplayName)
                          .Append(".");
                    }
                }
                
                // Get the base name of the this.SymbolType.
                int gravisIndex = this.SymbolType.Name.IndexOf('`'); // Indicates beginning of the generic type parameter portion of the name.
                sb.Append(gravisIndex == -1 ? this.SymbolType.Name : this.SymbolType.Name.Substring(0, gravisIndex));
                
                if (genericTypeSpecifiers.Count != 0)
                {
                    sb.Append('<');
                    sb.AppendJoin(", ", genericTypeSpecifiers.Select(t => t.ToSymbol().DisplayName));
                    sb.Append('>');
                }

                this._displayName = sb.ToString();
                return this._displayName;
            }
        }

        public IEnumerable<TypeSymbol> GetInheritanceHierarchy()
        {
            if (this.SymbolType is null)
            {
                throw new InvalidOperationException("SymbolType is null.");
            }
            for (Type? current = this.SymbolType; current != null; current = current.BaseType)
            {
                yield return current.ToSymbol();
            }
        }

        public string MsDocsUrl(string msDocsBaseUrl = "https://docs.microsoft.com/en-us/")
        {
            if (this.SymbolType is null)
            {
                throw new InvalidOperationException("SymbolType is null.");
            }
            if (!this.SymbolType.Assembly.IsSystemAssembly())
            {
                throw new InvalidOperationException($"{this.SymbolType.FullName} is not a system assembly.");
            }

            return (this.SymbolType.FullName is null, this.SymbolType.Namespace is null) switch
            {
                (true, true) => // Navigate to search specifying that we are looking for API
                    $"{msDocsBaseUrl}search/?terms=api%20{this.SymbolType.Name}&category=Reference&scope=.NET",
                (true, false) => // Try to reconstruct from namespace, not guaranteed to hit.
                    $"{msDocsBaseUrl}dotnet/api/{this.SymbolType.Namespace!.ToLower().Replace('`', '-')}.{this.SymbolType.Name.ToLower().Replace('`', '-')}",
                _ => // Navigate to documentation
                    $"{msDocsBaseUrl}dotnet/api/{this.SymbolType.FullName!.ToLower().Replace('`', '-')}"
            };
        }

        public string GetInternalDocsUrl(TypeSymbol? relativeTo = null)
        {
            if (this.SymbolType is null)
            {
                throw new InvalidOperationException("SymbolType is null.");
            }

            return Path.GetRelativePath(relativeTo is null ? s_virtualDeviceRoot : Path.Combine(s_virtualDeviceRoot, relativeTo.FullFilePathAndName),
                                        Path.Combine(s_virtualDeviceRoot, this.FilePath));
        }

        public MarkdownLink GetDocsLink()
        {
            string url = this.SymbolType.Assembly.IsSystemAssembly()
                            ? this.MsDocsUrl()
                            : this.GetInternalDocsUrl();

            return new MarkdownLink(this.DisplayName.FormatChevrons(), url);
        }

#endregion

    }
}
