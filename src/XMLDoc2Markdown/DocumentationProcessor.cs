using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using GlobExpressions;

using Markdown;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown
{
    internal static class DocumentationProcessor
    {
        public static void WriteCurrentProjectConfiguration()
        {
            using (WorkingDirectoryContext.Modulate(Path.GetDirectoryName(Project.Configuration.ProjectFilePath)))
            {
                EnsureDirectory(Project.Configuration.Current.Properties.Output.Path);

                Project.Project project = Project.Configuration.Current;

                TypeSymbolResolver.Instance.Add("index", ".\\", project.Properties.Index.Name);
                IMarkdownDocument indexPage = new MarkdownDocument().AppendHeader("Index", 1);

                var resolver = new BroadPathAssemblyResolver(project.Assembly.Select(a => a.File));

                Parallel.For(0, project.Assembly.Length, index =>
                {
                    ProcessAssembly(index, indexPage, resolver);
                });

                File.WriteAllText(
                    Path.Combine(Project.Configuration.Current.Properties.Output.Path, TypeSymbolResolver.Instance["index"].FilePath),
                    indexPage.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ProcessAssembly(int assemblyIndex, IMarkdownDocument indexPage, PathAssemblyResolver resolver)
        {
            Project.Project project = Project.Configuration.Current;
            string assemblyFilePath = Path.GetFullPath(project.Assembly[assemblyIndex].File);
            
            Func<string?, bool> namespaceFilter = s => !string.IsNullOrEmpty(s);
            string namespaceMatch = project.Properties.NamespaceMatch;
            if (namespaceMatch.IsValidRegex())
            {
                namespaceFilter = s => !string.IsNullOrEmpty(s) && Regex.IsMatch(s, project.Properties.NamespaceMatch);
            }
            else if (project.Properties.NamespaceMatch.IsGlobExpression())
            {
                namespaceFilter = s => !string.IsNullOrEmpty(s) && Glob.IsMatch(s, project.Properties.NamespaceMatch);
            }

            // Initialize assembly load context
            using var assemblyLoadContext = new MetadataLoadContext(resolver);

            CopyAssemblyReferences(project.Assembly[assemblyIndex]);

            // Load assembly
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyFilePath);

            // Parse xml documentation
            XmlDocumentation documentation = new XmlDocumentation(assemblyFilePath).Load();

            // Append header to index page
            indexPage.AppendHeader("Assembly - " + assembly.GetName().Name, 2);
            Project.IndexHeader headerInfo = project.Assembly[assemblyIndex].IndexHeader;
            IMarkdownBlockElement? headerContent = null;
            if (File.Exists(headerInfo.File))
            {
                headerContent = Extensions.MarkdownDocumentExtensions.BlockFromString(File.ReadAllText(headerInfo.File));
            }
            else if (headerInfo.Text != null && headerInfo.Text.Length != 0)
            {
                headerContent = Extensions.MarkdownDocumentExtensions.BlockFromString(string.Join("\r\n", headerInfo.Text));
            }
            indexPage.Append(headerContent);

            // Filter CompilerGenerated classes such as "<>c__DisplayClass"s, or things spawned by Source Generators
            TypeSymbolResolver.Instance.Add(LoadAssemblyTypes(assembly)
               .Where(t => t.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType == typeof(CompilerGeneratedAttribute)) is null && t.Namespace != null)
               .Select(t => t.GetTypeInfo()));

            foreach (IGrouping<string?, TypeSymbol> typeNamespaceGrouping in TypeSymbolResolver
               .Instance
               .Select(kvp => kvp.Value)
               .GroupBy(type => type.SymbolType?.Namespace)
               .Where(g => namespaceFilter(g.Key))
               .OrderBy(g => g.Key))
            {
                string documentationDir = typeNamespaceGrouping.First().Directory;
                EnsureDirectory(documentationDir);

                indexPage.AppendHeader(typeNamespaceGrouping.Key, 3);
                indexPage.Append(ProcessAssemblyNamespace(typeNamespaceGrouping!, Project.Configuration.Current.Properties.Output.Path, documentation, assembly));
            }
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
                var sb = new StringBuilder(1000);
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

        private static void CopyAssemblyReferences(Project.Assembly assembly)
        {
            string? targetDirectory = Path.GetDirectoryName(assembly.File);
            if (targetDirectory is null)
            {
                throw new DirectoryNotFoundException($"Could not find parent directory for reference '{assembly.File}'.");
            }

            string[]? assemblyReferences = assembly.References?.AssemblyReference;
            if (assemblyReferences != null)
            {
                foreach (string sourceFilePath in assemblyReferences)
                {
                    string targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(sourceFilePath));
                    if (!File.Exists(targetFilePath))
                    {
                        File.Copy(sourceFilePath, targetFilePath);
                        LoadAssemblyReferenceDocumentation(targetFilePath);
                    }
                }
            }

            string[]? nugetReferences = assembly.References?.NugetReference;
            if (nugetReferences != null)
            {
                string tempoaryDirectory = Path.Combine(Path.GetTempPath(), "XMLDoc2Markdown", Path.GetFileNameWithoutExtension(assembly.File) + '\\');
                EnsureDirectory(tempoaryDirectory);

                foreach (string sourceFilePath in nugetReferences)
                {
                    string nugetFileName = Path.GetFileName(sourceFilePath);
                    string nugetExtractedFilePath = Path.Combine(tempoaryDirectory, nugetFileName + '\\');
                    File.Copy(sourceFilePath, nugetExtractedFilePath);

                    // Extract nuget package
                    string nugetFilePath = Path.Combine(tempoaryDirectory, nugetFileName);
                    EnsureDirectory(nugetFilePath);

                    using (var fs = new FileStream(nugetExtractedFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        var nugetArchive = new ZipArchive(fs);
                        nugetArchive.ExtractToDirectory(nugetFilePath);
                    }

                    // Glob for all dlls and copy directory content
                    foreach (FileSystemInfo fsi in Glob.Files(nugetFilePath, @"**\*.dll")
                       .Select(Path.GetDirectoryName)
                       .Distinct()
                       .Where(Directory.Exists)
                       .SelectMany(d => new DirectoryInfo(d!).GetFileSystemInfos()))
                    {
                        if (fsi is FileInfo file)
                        {
                            string targetFilePath = Path.Combine(targetDirectory, file.Name);
                            file.CopyTo(targetFilePath);
                            LoadAssemblyReferenceDocumentation(targetFilePath);
                        }
                    }
                }

                Directory.Delete(tempoaryDirectory);
            }
        }

        private static void LoadAssemblyReferenceDocumentation(string dllFilePath)
        {

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
