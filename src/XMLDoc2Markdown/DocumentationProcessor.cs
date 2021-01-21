using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using GlobExpressions;

using Markdown;

using XMLDoc2Markdown.AssemblyHelpers;
using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Project;

using Assembly = System.Reflection.Assembly;

namespace XMLDoc2Markdown
{
    internal static class DocumentationProcessor
    {
        [STAThread]
        public static void WriteCurrentProjectConfiguration(string outputPath)
        {
            Project.Project project = Configuration.Current;

            TypeSymbolProvider.Instance.Add("index", outputPath, project.Properties.Index.Name);
            IMarkdownDocument indexPage = new MarkdownDocument().AppendHeader("Index", 1);

            for (int index = 0; index < project.Assembly.Length; index++)
            {
                ProcessAssembly(index, outputPath, indexPage, out WeakReference alcWeakReference);
                for (int i = 0; alcWeakReference.IsAlive && i < 128; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            File.WriteAllText(TypeSymbolProvider.Instance["index"].FilePath, indexPage.ToString());
        }

        [STAThread]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ProcessAssembly(int assemblyIndex, string outputPath, IMarkdownDocument indexPage, out WeakReference loadContextWeakReference)
        {
            Project.Project project = Configuration.Current;
            string assemblyFilePath = project.Assembly[assemblyIndex].File;

            Func<string?, bool> namespaceFilter = _ => true;
            string namespaceMatch = project.Properties.NamespaceMatch;
            if (namespaceMatch.IsValidRegex())
            {
                namespaceFilter = s => Regex.IsMatch(s, project.Properties.NamespaceMatch);
            }
            else if (project.Properties.NamespaceMatch.IsGlobExpression())
            {
                namespaceFilter = s => Glob.IsMatch(s, project.Properties.NamespaceMatch);
            }

            // Initialize assembly load context
            var assemblyLoadContext = new HostAssemblyLoadContext(assemblyFilePath);
            loadContextWeakReference = new WeakReference(assemblyLoadContext);

            // Copy references
            string[]? assemblyReferences = project.Assembly[assemblyIndex].References?.AssemblyReference;
            if (assemblyReferences != null)
            {
                foreach (string sourceFilePath in assemblyReferences)
                {
                    string targetFilePath = Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, Path.GetFileName(sourceFilePath));
                    if (!File.Exists(targetFilePath))
                    {
                        File.Copy(sourceFilePath, targetFilePath);
                    }
                }
            }

            string[]? nugetReferences = project.Assembly[assemblyIndex].References?.NugetReference;
            if (nugetReferences != null)
            {
                string tempoaryDirectory = Path.Combine(Path.GetTempPath(), "XMLDoc2Markdown", Path.GetFileNameWithoutExtension(assemblyFilePath) + '\\');
                foreach (string sourceFilePath in nugetReferences)
                {
                    string nugetFileName = Path.GetFileName(sourceFilePath);
                    string nugetExtractedFilePath = Path.Combine(tempoaryDirectory, nugetFileName + '\\');
                    File.Copy(sourceFilePath, nugetExtractedFilePath);

                    // Extract nuget package
                    string nugetFilePath = Path.Combine(tempoaryDirectory, nugetFileName);
                    using (FileStream fs = new FileStream(nugetExtractedFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        ZipArchive nugetFile = new ZipArchive(fs);
                        nugetFile.ExtractToDirectory(nugetFilePath);
                    }

                    // Glob for all dlls and copy directory content
                    string targetFilePath = Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, Path.GetFileName(sourceFilePath));
                    foreach (FileSystemInfo fsi in Glob.Files(nugetFilePath, @"**\*.dll")
                       .Select(Path.GetDirectoryName)
                       .Distinct()
                       .Where(Directory.Exists)
                       .SelectMany(d => new DirectoryInfo(d!).GetFileSystemInfos()))
                    {
                        if (fsi is FileInfo file)
                        {
                            file.CopyTo(Path.Combine(targetFilePath, file.Name));
                        }
                    }
                }

                Directory.Delete(tempoaryDirectory);
            }

            // Load assembly
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyFilePath);

            var documentation = new XmlDocumentation(assemblyFilePath);

            indexPage.AppendHeader(assembly.GetName().Name, 2);

            // Filter CompilerGenerated classes such as "<>c__DisplayClass"s, or things spawned by Source Generators
            TypeSymbolProvider.Instance.Add(LoadAssemblyTypes(assembly).Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() is null && t.Namespace != null));

            foreach (IGrouping<string?, TypeSymbol> typeNamespaceGrouping in TypeSymbolProvider
               .Instance
               .Select(kvp => kvp.Value)
               .GroupBy(type => type.SymbolType.Namespace)
               .Where(g => namespaceFilter(g.Key))
               .OrderBy(g => g.Key))
            {
                string documentationDir = typeNamespaceGrouping.First().Directory;
                EnsureDirectory(documentationDir);

                indexPage.AppendHeader(typeNamespaceGrouping.Key, 3);
                indexPage.Append(ProcessAssemblyNamespace(typeNamespaceGrouping!, outputPath, documentation, assembly));
            }

            // Unload assembly
            assemblyLoadContext.Unload();
        }

        private static Type[] LoadAssemblyTypes(Assembly assembly)
        {
            Type[] assemblyTypes;

            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder(1000);
                sb.AppendLine("ReflectionTypeLoadException: Occurs when dynamically linked assemblies cannot be loaded, they have to be imported manually. For more info see: https://natemcmaster.com/blog/2017/12/21/netcore-primitives/#depsjson and https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing");
                if (!(ex.LoaderExceptions is null))
                {
                    sb.AppendLine($"\r\n\r\n LoaderExceptions: FileNotFoundExceptions: {ex.LoaderExceptions.Count(x => x is FileNotFoundException)} -----");
                    foreach (FileNotFoundException subEx in ex.LoaderExceptions.Where(x => x is FileNotFoundException).Cast<FileNotFoundException>())
                    {
                        string log = subEx.FusionLog ?? subEx.Message;
                        if (!string.IsNullOrWhiteSpace(log))
                        {
                            Console.WriteLine(log);
                        }
                    }

                    sb.AppendLine($"\r\n\r\n LoaderExceptions: other Exceptions: {ex.LoaderExceptions.Count(x => !(x is FileNotFoundException))} -----");
                    foreach (Exception? subEx in ex.LoaderExceptions.Where(x => !(x is FileNotFoundException)))
                    {
                        if (subEx != null)
                        {
                            sb.AppendLine(subEx.Message);
                        }
                    }
                }

                Console.WriteLine(sb);
                throw;
            }

            return assemblyTypes;
        }

        private static MarkdownList ProcessAssemblyNamespace(IGrouping<string, TypeSymbol> typeNamespaceGrouping, string outputPath, XmlDocumentation documentation, Assembly assembly)
        {
            var list = new MarkdownList();
            foreach (TypeSymbol symbol in typeNamespaceGrouping
               .OrderBy(t => t.SymbolType!.Name))
            {
                string fullFileName = Path.Combine(outputPath, symbol.FilePath);
                EnsureDirectory(Path.GetDirectoryName(fullFileName));

                list.AddItem(new MarkdownLink(new MarkdownInlineCode(symbol.DisplayName), symbol.FilePath));

                File.WriteAllText(fullFileName, new TypeDocumentation(assembly, symbol, documentation).ToString());
            }

            return list;
        }

        private static void EnsureDirectory(string? directoryName)
        {
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }
}
