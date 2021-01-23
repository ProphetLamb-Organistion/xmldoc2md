using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace XMLDoc2Markdown.Utility
{
    /// <summary>
    /// Includes all assemblies in the directory of each assembly path, additionally includes framework assemblies for resolving.
    /// </summary>
    public class BroadPathAssemblyResolver : PathAssemblyResolver
    {
        public BroadPathAssemblyResolver(IEnumerable<string> assemblyPaths)
            : base(CollectBroadAssemblyFilePaths(assemblyPaths))
        {
        }

        private static readonly IReadOnlyList<string> s_frameworkAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        protected static IReadOnlyList<string> CollectBroadAssemblyFilePaths(IEnumerable<string> assemblyFilePaths)
        {
            var collector = new List<string>(s_frameworkAssemblies);
            foreach (IEnumerable<FileInfo> assemblies in assemblyFilePaths
               .Select(f => new FileInfo(f).Directory)
               .Where(di => di != null && di.Exists)
               .Distinct()
               .Select(di => di!.EnumerateFiles("*.dll")))
            {
                collector.AddRange(assemblies.Select(fi => fi.FullName));
            }
            return collector;
        }
    }
}
