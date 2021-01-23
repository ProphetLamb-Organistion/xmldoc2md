using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown
{
    internal class TypeSymbolProvider : IReadOnlyDictionary<string, TypeSymbol>
    {
#region Fields

        private readonly IDictionary<string, TypeSymbol> _symbolProvider = new Dictionary<string, TypeSymbol>();

        private static readonly Lazy<TypeSymbolProvider> s_instance = new Lazy<TypeSymbolProvider>(() => new TypeSymbolProvider());

#endregion

#region Properties

        public static TypeSymbolProvider Instance => s_instance.Value;

        public IEnumerable<string> Keys => this._symbolProvider.Keys;
        public IEnumerable<TypeSymbol> Values => this._symbolProvider.Values;

        public int Count => this._symbolProvider.Count;
        public TypeSymbol this[string symbolIdentifier] => this._symbolProvider[symbolIdentifier];

#endregion

#region Public members

        public void Add(IEnumerable<TypeInfo> types)
        {
            IDictionary<string, string> typeNamespaceToPathMap = new Dictionary<string, string>();
            // Namespace directory structure
            foreach ((string nspc, string dir) in new NamespaceDirectoryTree(types.Where(t => t.Namespace != null).Select(t => t.Namespace!)))
            {
                typeNamespaceToPathMap.TryAdd(nspc, dir);
            }

            // TypeSymbols
            foreach (KeyValuePair<string, TypeSymbol> keyValuePair in types
               .Select(x => KeyValuePair.Create(x.GetTypeSymbolIdentifier(), new TypeSymbol(x, typeNamespaceToPathMap[x.Namespace!], StringExtensions.MakeTypeNameFileNameSafe(x.Name)))))
            {
                this._symbolProvider.Add(keyValuePair);
            }
        }

        public void Add(string symbolIdentifer, string filePath, string fileNameWithoutExtension) => this._symbolProvider.Add(symbolIdentifer, new TypeSymbol(filePath, fileNameWithoutExtension));

        public bool ContainsKey(string symbolIdentifier) => this._symbolProvider.ContainsKey(symbolIdentifier);

        public bool TryGetValue(string symbolIdentifier, [MaybeNullWhen(false)] out TypeSymbol value) => this._symbolProvider.TryGetValue(symbolIdentifier, out value);

        public IEnumerator<KeyValuePair<string, TypeSymbol>> GetEnumerator() => this._symbolProvider.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

#endregion

#region Private members

        private IDictionary<string[], char[]> NamespacesToDirectoryStructure(IEnumerable<string[]> namespaces)
        {
            IDictionary<string[], char[]> typeNamespaceToPathMap = new Dictionary<string[], char[]>();
            IList<string[]> processedTypeNamespaces = new List<string[]>();
            foreach (string[] segments in namespaces
               .Distinct(new StringSequenceEqualityComparer())
               .OrderBy(n => n.Length))
            {
                int segment = 0;
                char[] path = string.Join('.', segments).ToCharArray();
                IList<int> sharedNamespacePortionIndices;
                IList<string[]> sharedNamespacePortions = processedTypeNamespaces;
                do
                {
                    // All namespaces, that share the segment and all previous with this.
                    sharedNamespacePortionIndices = this.IndexOfAll(sharedNamespacePortions, segments, segment).ToList();
                    if (segment != 0)
                    {
                        // All namespaces, that share all previous segments with this.
                        IList<int> indices = sharedNamespacePortionIndices;
                        IEnumerable<string[]> removedNamespaces = sharedNamespacePortions.Where((x, i) => !indices.Contains(i));
                        if (removedNamespaces.Any())
                        {
                            // Convert the [.] to [\] to indicate a path branching for all that share previous segments
                            int charIndex = segments.Take(segment).Select(s => s.Length).Aggregate(0, (x, y) => x + y + 1) - 1;
                            path[charIndex] = '\\';
                            foreach (char[] value in typeNamespaceToPathMap
                               .Where(kvp => kvp.Value.Length > charIndex && (ReferenceEquals(kvp.Key, segments) || sharedNamespacePortions.Contains(kvp.Key)))
                               .Select(kvp => kvp.Value))
                            {
                                value[charIndex] = '\\';
                            }
                        }
                    }

                    sharedNamespacePortions = sharedNamespacePortions.Where((x, i) => sharedNamespacePortionIndices.Contains(i)).ToList();
                    segment++;
                } while (segment < segments.Length && sharedNamespacePortions.Count > 0);

                processedTypeNamespaces.Add(segments);
                typeNamespaceToPathMap.Add(segments, path);
            }

            return typeNamespaceToPathMap;
        }

        /// <summary>
        ///     Enumerates all indices in the collection where the segment is equal to the segment at the same position -
        ///     specified by segment - in test.
        /// </summary>
        private IEnumerable<int> IndexOfAll(IList<string[]> collection, string[] test, int segment)
        {
            for (int index = 0; index < collection.Count; index++)
            {
                if (collection[index].Length > segment && String.Equals(test[segment], collection[index][segment], StringComparison.Ordinal))
                {
                    yield return index;
                }
            }
        }

        private class StringSequenceEqualityComparer : IEqualityComparer<string[]?>
        {
            public bool Equals(string[]? x, string[]? y) => !(x is null) && x.Length == y?.Length && x.SequenceEqual(y, StringComparer.Ordinal);

            public int GetHashCode(string[]? obj) => obj?.GetHashCode() ?? 0;
        }

#endregion
    }
}
