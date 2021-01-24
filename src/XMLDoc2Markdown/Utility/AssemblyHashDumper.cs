using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using XMLDoc2Markdown.Extensions;

namespace XMLDoc2Markdown.Utility
{
    internal static class AssemblyHashDumper
    {
        public static IEnumerable<int> AssemblyNameHashes(IEnumerable<string> assemblyFileNames, Action<string> exceptionHandle)
        {
            foreach (string fullFileName in assemblyFileNames.Distinct(FileNameEqualityComparer.Instance))
            {
                string? name = null;

                using (FileStream sr = File.OpenRead(fullFileName))
                {
                    var reader = new PEReader(sr);
                    if (reader.HasMetadata)
                    {
                        MetadataReader metadataReader = reader.GetMetadataReader();
                        StringHandle nameHandle = metadataReader.GetModuleDefinition().Name;
                        name = Path.GetFileNameWithoutExtension(metadataReader.GetString(nameHandle)).ToLowerInvariant();
                    }
                }

                if (name != null)
                {
                    yield return AssemblyNameHashFunction(name);
                }
            }
        }

        public static int AssemblyNameHashFunction(string assemblyName)
        {
            const int p = 31;
            const int m = (int)(1e9 + 9);
            const long a = 'a';
            long hashValue = 0;
            long pPow = 1;
            foreach (char t in assemblyName)
            {
                hashValue = (hashValue + (((t - a) + 1) * pPow)) % m;
                pPow = (pPow * p) % m;
            }

            return (int)hashValue;
        }

        public class FileNameEqualityComparer : IEqualityComparer<string>
        {
            private static readonly Lazy<FileNameEqualityComparer> s_instance = new Lazy<FileNameEqualityComparer>(() => new FileNameEqualityComparer());
            public static FileNameEqualityComparer Instance => s_instance.Value;

            public bool Equals(string? x, string? y) => Path.GetFileNameWithoutExtension(x)!.Equals(Path.GetFileNameWithoutExtension(y), StringComparison.Ordinal);

            public int GetHashCode([DisallowNull] string obj) => Path.GetFileNameWithoutExtension(obj).GetHashCode();
        }
    }
}
