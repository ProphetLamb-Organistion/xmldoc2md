/*
 * All mechanisms related to Assembly loading & unloading are implemented by Jan Vorlicek, on Github https://github.com/dotnet/samples/tree/master/core/tutorials/Unloading
 * and are licensed under the CC BY 4.0 https://github.com/dotnet/samples/blob/master/LICENSE license.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

using GlobExpressions;

namespace XMLDoc2Markdown.AssemblyHelpers
{
    // This is a collectible (unloadable) AssemblyLoadContext that loads the dependencies
    // of the plugin from the plugin's binary directory.
    internal class HostAssemblyLoadContext : AssemblyLoadContext
    {
        // Resolver of the locations of the assemblies that are dependencies of the
        // main plugin assembly.
        protected AssemblyDependencyResolver _resolver;

        public HostAssemblyLoadContext(string pluginPath)
            : base(true) => this._resolver = new AssemblyDependencyResolver(pluginPath);

        // The Load method override causes all the dependencies present in the plugin's binary directory to get loaded
        // into the HostAssemblyLoadContext together with the plugin assembly itself.
        // NOTE: The Interface assembly must not be present in the plugin's binary directory, otherwise we would
        // end up with the assembly being loaded twice. Once in the default context and once in the HostAssemblyLoadContext.
        // The types present on the host and plugin side would then not match even though they would have the same names.
        protected override Assembly Load(AssemblyName name)
        {
            string? assemblyPath = this._resolver.ResolveAssemblyToPath(name);
            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                return this.LoadFromAssemblyPath(assemblyPath);
            }

            assemblyPath = DetermineFrameworkAssemblyFilePath(name);
            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                return this.LoadFromAssemblyPath(assemblyPath);
            }

            throw new InvalidOperationException("The assembly could not be loaded.");
        }

        private static string? DetermineFrameworkAssemblyFilePath(AssemblyName name)
        {
            // Try to load a framework assembly 
            // Multiple candidates with different versions might be found
            // Try to find candidate with same version, fallback to higher version.
            string namePattern = $"**\\{name.Name}.dll";
            string[] candidates = Settings.Instance.FrameworkAssemblyPaths
               .SelectMany(p => Glob.Files(p, namePattern)
                   .Select(f => Path.Combine(p, f)))
               .ToArray();

            if (candidates.Length == 0)
            {
                return null;
            }
            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            Version? version = name.Version;
            KeyValuePair<Version?, string>[] orderedCandidates = candidates
               .Select(c => KeyValuePair.Create(LooseVersion(Path.GetFileName(Path.GetDirectoryName(c))), c))
               .Where(kvp => kvp.Key != null)
               .OrderBy(kvp => kvp.Key)
               .ToArray();
            if (orderedCandidates.Length == 0)
            {
                return candidates.Last();
            }
            if (version is null || version.Major == 0 && version.Minor == 0)
            {
                return orderedCandidates[0].Value;
            }
            string greaterVersion = orderedCandidates.FirstOrDefault(kvp => version.CompareTo(kvp.Key) <= 0).Value;
            if (!string.IsNullOrWhiteSpace(greaterVersion))
            {
                return greaterVersion;
            }

            return orderedCandidates[0].Value;

        }
        
        private static Version? LooseVersion(string? looseVersionString)
        {
            if (looseVersionString is null)
            {
                return null;
            }

            MatchCollection matches = Regex.Matches(looseVersionString, @"(\d+\.)?(\d+\.)?(\*|\d+)");
            if (matches.Count == 0 || matches[0].Groups.Count <= 3)
            {
                return null;
            }
            
            int major = Math.Abs(int.Parse(matches[0].Groups[1].Value.AsSpan()[0..^1]));
            int minor = Math.Abs(int.Parse(matches[0].Groups[2].Value.AsSpan()[0..^1]));
            int build = Math.Abs(int.Parse(matches[0].Groups[3].Value));
            return new Version(major, minor, build);
        }
    }
}
