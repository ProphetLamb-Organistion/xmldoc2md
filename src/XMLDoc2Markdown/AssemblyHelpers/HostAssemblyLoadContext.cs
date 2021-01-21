/*
 * All mechanisms related to Assembly loading & unloading are implemented by Jan Vorlicek, on Github https://github.com/dotnet/samples/tree/master/core/tutorials/Unloading
 * and are licensed under the CC BY 4.0 https://github.com/dotnet/samples/blob/master/LICENSE license.
 */

using System;
using System.Reflection;
using System.Runtime.Loader;

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
            if (assemblyPath != null)
            {
                return this.LoadFromAssemblyPath(assemblyPath);
            }

            throw new InvalidOperationException("The assembly could not be loaded.");
        }
    }
}
