using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;

using GlobExpressions;

using Markdown;
using Microsoft.Extensions.CommandLineUtils;

namespace XMLDoc2Markdown
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Debugger.Launch();
            string solutionRoot = Environment.CurrentDirectory;
            DirectoryInfo parent = null;
            while (!(solutionRoot is null) && !String.Equals(parent?.Name, "xmldoc2md", StringComparison.InvariantCultureIgnoreCase))
            {
                parent = new DirectoryInfo(solutionRoot).Parent;
                solutionRoot = parent?.FullName;
            }
            if (solutionRoot is null)
                throw new Exception();
            args = new[] { Path.Combine(solutionRoot, @"publish\MyClassLib.dll"), Path.Combine(solutionRoot, @"docs\sample") };
            //args[0] = @"C:\Users\Public\source\repos\Groundbeef\src\**\bin\**\*.dll";
            //args[1] = Path.Combine(solutionRoot, @"docs\Groundbeef");
#endif
            var app = new CommandLineApplication
            {
                Name = "xmldoc2md"
            };

            app.VersionOption("-v|--version", () => $"Version {Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
            app.HelpOption("-?|-h|--help");

            CommandArgument srcArg = app.Argument("src", "DLL source path");
            CommandArgument outArg = app.Argument("out", "Output directory");

            CommandOption namespaceMatchOption = app.Option(
                "--namespace-match <regex>",
                "Regex pattern to select namespaces",
                CommandOptionType.SingleValue);

            CommandOption indexPageNameOption = app.Option(
                "--index-page-name <regex>",
                "Name of the index page (default: \"index\")",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                string src = srcArg.Value;
                string @out = outArg.Value;
                string namespaceMatch = namespaceMatchOption.Value();
                string indexPageName = indexPageNameOption.HasValue() ? indexPageNameOption.Value() : "index";

                if (string.IsNullOrWhiteSpace(namespaceMatch))
                {
                    namespaceMatch = null;
                }
                else if (!namespaceMatch.IsValidRegex())
                {
                    throw new ArgumentException("The RegEx pattern namespace-match is invalid.");
                }

                string[] sourceFiles = src.GetGlobFiles().ToArray();
                if (sourceFiles.Length > 1)
                {
                    string executableFileName = DetermineCurrentProcessFullFilePath();
                    foreach (string sourceFile in sourceFiles)
                    {
                        StringBuilder arguments = new StringBuilder();
                        arguments.Append(sourceFile).Append(" ").Append(@out);
                        if (!(namespaceMatch is null))
                        {
                            arguments.Append(" --namespace-match ").Append(namespaceMatch);
                        }
                        arguments.Append(" --index-page-name ").Append(indexPageName);
                        Process.Start(executableFileName, arguments.ToString());
                    }
                    
                }
                if (sourceFiles.Length == 0)
                {
                    Console.WriteLine("No files found that match the glob pattern provided.");
                    return 1;
                }
                try
                {
                    ProcessAssembly(Assembly.LoadFrom(sourceFiles[0]), new XmlDocumentation(sourceFiles[0]), @out, namespaceMatch, indexPageName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to process the assembly at {0}.\r\n An error occured in {1}\r\nMessage: {2}\r\n{3}\r\n{4}", sourceFiles[0], ex.Source, ex.Message, ex.TargetSite, ex.StackTrace);
                    return -1;
                }
                return 0;
            });

            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to execute application.\r\n An error occured in {0}\r\nMessage: {1}\r\n{2}\r\n{3}", ex.Source, ex.Message, ex.TargetSite, ex.StackTrace);
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
                StringBuilder sb = new StringBuilder(1000);
                sb.AppendLine("ReflectionTypeLoadException: Occurs when dynamically linked assemblies cannot be loaded, they have to be imported manually. For more info see: https://natemcmaster.com/blog/2017/12/21/netcore-primitives/#depsjson and https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing");
                if (!(ex.LoaderExceptions is null))
                {
                    sb.AppendLine($"\r\n\r\n LoaderExceptions: FileNotFoundExceptions: {ex.LoaderExceptions.Count(x => x is FileNotFoundException)} -----");
                    foreach (FileNotFoundException subEx in ex.LoaderExceptions.Where(x => x is FileNotFoundException).Cast<FileNotFoundException>())
                    {
                        string log = subEx.FusionLog ?? subEx.Message;
                        if (!String.IsNullOrWhiteSpace(log))
                            Console.WriteLine(log);
                    }
                    sb.AppendLine($"\r\n\r\n LoaderExceptions: other Exceptions: {ex.LoaderExceptions.Count(x => !(x is FileNotFoundException))} -----");
                    foreach (Exception subEx in ex.LoaderExceptions.Where(x => !(x is FileNotFoundException)))
                    {
                        sb.AppendLine(subEx.Message);
                    }
                }
                Console.WriteLine(sb);
                throw;
            }

            return assemblyTypes;
        }

        private static MarkdownList ProcessAssemblyNamespace(IGrouping<string, Type> typeNamespaceGrouping, string outputPath, XmlDocumentation documentation, Assembly assembly)
        {
            var list = new MarkdownList();
            foreach (Type type in typeNamespaceGrouping
                                 .OrderBy(t => t.Name))
            {
                string beautifyName = type.GetDisplayName();
                string fileName = $"{StringExtensions.MakeTypeNameFileNameSafe(beautifyName)}.md";
                list.AddItem(new MarkdownLink(new MarkdownInlineCode(beautifyName), typeNamespaceGrouping.Key + "/" + fileName));

                File.WriteAllText(Path.Combine(outputPath, typeNamespaceGrouping.Key, fileName), new TypeDocumentation(assembly, type, documentation).ToString());
            }
            return list;
        }

        private static void ProcessAssembly(Assembly assembly, XmlDocumentation documentation, string outputPath, string namespaceMatch, string indexPageName)
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            IMarkdownDocument indexPage = new MarkdownDocument().AppendHeader(assembly.GetName().Name, 1);

            foreach (IGrouping<string, Type> typeNamespaceGrouping in LoadAssemblyTypes(assembly)
                                                                     .Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() is null) // Filter CompilerGenerated classes such as "<>c__DisplayClass"s, or things spawned by Source Generators
                                                                     .GroupBy(type => type.Namespace)
                                                                     .Where(g =>  !(g.Key is null) && (namespaceMatch is null || Regex.IsMatch(g.Key, namespaceMatch)))
                                                                     .OrderBy(g => g.Key))
            {
                string subDir = Path.Combine(outputPath, typeNamespaceGrouping.Key);
                if (!Directory.Exists(subDir))
                {
                    Directory.CreateDirectory(subDir);
                }

                indexPage.AppendHeader(new MarkdownInlineCode(typeNamespaceGrouping.Key), 2);
                indexPage.Append(ProcessAssemblyNamespace(typeNamespaceGrouping, outputPath, documentation, assembly));
            }

            File.WriteAllText(Path.Combine(outputPath, $"{indexPageName}.md"), indexPage.ToString());
        }

        private static string DetermineCurrentProcessFullFilePath()
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Cannot get the main module of the current process.");
        }
    }
}
