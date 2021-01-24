using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown
{
    internal class TypeSymbolResolver : IReadOnlyDictionary<string, TypeSymbol>
    {
#region Fields

        private readonly ConcurrentDictionary<string, TypeSymbol> _symbolProvider = new ConcurrentDictionary<string, TypeSymbol>();

        private static readonly Lazy<TypeSymbolResolver> s_instance = new Lazy<TypeSymbolResolver>(() => new TypeSymbolResolver());

#endregion

#region Properties

        public static TypeSymbolResolver Instance => s_instance.Value;

        public IEnumerable<string> Keys => this._symbolProvider.Keys;
        public IEnumerable<TypeSymbol> Values => this._symbolProvider.Values;

        public int Count => this._symbolProvider.Count;
        public TypeSymbol this[string symbolIdentifier] => this._symbolProvider[symbolIdentifier];

#endregion

#region Public members

        public int Add(IEnumerable<TypeInfo> types)
        {
            IDictionary<string, string> typeNamespaceToPathMap = new Dictionary<string, string>();
            // Namespace directory structure
            foreach ((string nspc, string dir) in new NamespaceDirectoryTree(types.Where(t => t.Namespace != null).Select(t => t.Namespace!)))
            {
                typeNamespaceToPathMap.TryAdd(nspc, dir);
            }

            // TypeSymbols
            int counter = 0;
            foreach ((string key, TypeSymbol value) in types
               .Select(x => KeyValuePair.Create(x.GetTypeSymbolIdentifier(), new TypeSymbol(x, typeNamespaceToPathMap[x.Namespace!], StringExtensions.MakeTypeNameFileNameSafe(x.Name)))))
            {
                this._symbolProvider.TryAdd(key, value);
                counter++;
            }
            return counter;
        }

        public bool Add(string symbolIdentifier, string filePath, string fileNameWithoutExtension) => this._symbolProvider.TryAdd(symbolIdentifier, new TypeSymbol(filePath, fileNameWithoutExtension));

        public bool ContainsKey(string symbolIdentifier) => this._symbolProvider.ContainsKey(symbolIdentifier);

        public bool TryGetValue(string symbolIdentifier, [MaybeNullWhen(false)] out TypeSymbol value) => this._symbolProvider.TryGetValue(symbolIdentifier, out value);

        public IEnumerator<KeyValuePair<string, TypeSymbol>> GetEnumerator() => this._symbolProvider.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

#endregion
    }
}
