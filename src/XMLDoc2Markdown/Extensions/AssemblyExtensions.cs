using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown.Extensions
{
    internal static class AssemblyExtensions
    {
        internal static IEnumerable<string?> GetDeclaredNamespaces(this Assembly assembly)
        {
            return assembly.GetTypes().Select(type => type.Namespace).Distinct();
        }

        public static bool IsSystemAssembly(this Assembly assembly)
        {
            string? name = assembly.GetName().Name?.ToLowerInvariant();
            // Checking against typeof(string).Assembly is insufficient for many types e.g. PresentationCore types. Instead check that the assembly name hash against .Net Core/Framework assemblies
            return name != null && Settings.Instance.FrameworkAssemblyNameHashes.BinarySearch(AssemblyHashDumper.AssemblyNameHashFunction(name)) >= 0;
        }
    }
}
