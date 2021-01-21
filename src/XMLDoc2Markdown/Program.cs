using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Schema;

using GlobExpressions;

using Markdown;
using Microsoft.Extensions.CommandLineUtils;

using XMLDoc2Markdown.AssemblyHelpers;
using XMLDoc2Markdown.Extensions;

namespace XMLDoc2Markdown
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            Debugger.Launch();
            string? solutionRoot = Environment.CurrentDirectory;
            DirectoryInfo? parent = null;
            while (!(solutionRoot is null) && !String.Equals(parent?.Name, "xmldoc2md", StringComparison.InvariantCultureIgnoreCase))
            {
                parent = new DirectoryInfo(solutionRoot).Parent;
                solutionRoot = parent?.FullName;
            }
            if (solutionRoot is null)
            {
                throw new Exception();
            }
            //args = new[] { Path.Combine(solutionRoot, @"publish\MyClassLib.dll"), Path.Combine(solutionRoot, @"docs\sample") };
            //args[0] = @"C:\Users\Public\source\repos\GroupedObservableCollection\src\bin\Debug\netstandard2.0\GroupedObservableCollection.dll"; //@"C:\Users\Public\source\repos\Groundbeef\src\**\bin\**\*.dll";
            //args[1] = Path.Combine(solutionRoot, @"docs\GOC");
#endif
            var app = new CommandLineApplication
            {
                Name = "xmldoc2md"
            };

            app.VersionOption("-v|--version", () => $"Version {Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
            app.HelpOption("-?|-h|--help");

            CommandArgument outArg = app.Argument("out", "Output directory");
            
            CommandArgument srcArg = app.Argument("src", "DLL source path");

            CommandOption projectOption = app.Option(
                "-p <regex>|--project <regex>",
                "Filename of the project to generate",
                CommandOptionType.SingleValue);

            CommandOption namespaceMatchOption = app.Option(
                "-r--namespace-match <regex>",
                "Glob or regex pattern to select namespaces",
                CommandOptionType.SingleValue);

            CommandOption indexPageNameOption = app.Option(
                "--index-page-name <regex>",
                "Name of the index page (default: \"index\")",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                string? @out = outArg.Value;
                string? src = srcArg.Value;
                string? namespaceMatch = namespaceMatchOption.Value();
                string indexPageName = indexPageNameOption.HasValue() ? indexPageNameOption.Value() : "index";
                string? projectFileName = projectOption.Value();

                if (projectOption.HasValue())
                {
                    return ConfigureFromProject(projectFileName!, @out);
                }
                else
                {
                    if (@out is null)
                    {
                        throw new CommandParsingException(app, "out is undefined.");
                    }
                    EnsureDirectory(@out);

                    if (src is null)
                    {
                        throw new CommandParsingException(app, "src is undefined.");
                    }
                    if (!File.Exists(src))
                    {
                        throw new FileNotFoundException("src was not found. " + src);
                    }

                    return ConfigureFromFile(src, @out, namespaceMatch, indexPageName);
                }
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
                Console.WriteLine("Unable to execute application." + ex.ToLog());
            }
        }

        private static int ConfigureFromFile(string src, string @out, string? namespaceMatch, string indexPageName)
        {
            string[] targets = src.GetGlobFiles().ToArray();
            
            // Generate project configuration
            Project.Project project = Project.Configuration.Create();

            project.Properties = new Project.Properties
            {
                Index = new Project.Index {Name = namespaceMatch},
                NamespaceMatch = namespaceMatch,
                Output = new Project.Output {Path = @out}
            };

            project.Assembly = new Project.Assembly[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                project.Assembly[i] = new Project.Assembly
                {
                    Documentation = null,
                    File = targets[i],
                    IndexHeader = new Project.IndexHeader { File = null, Text = null },
                    References = null
                };
            }

            DocumentationProcessor.WriteCurrentProjectConfiguration(@out);
            return 0;
        }

        private static int ConfigureFromProject(string projectFileName, string @out)
        {
            if (!File.Exists(projectFileName))
            {
                Console.WriteLine("The specified project file could not be found. " + projectFileName);
                return -2;
            }

            // Prepare project configuration
            try
            {
                Project.Configuration.Load(projectFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("The project file count not be accessed. " + ex.ToLog(true));
                return -1;
            }
            catch (XmlSchemaValidationException ex)
            {
                Console.WriteLine("The project could no be loaded correctly. " + ex.ToLog(true));
                return -1;
            }
            if (!Project.Configuration.IsLoaded)
            {
                Console.WriteLine("The project could no be loaded correctly.");
                return -1;
            }

            DocumentationProcessor.WriteCurrentProjectConfiguration(@out);
            return 0;
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
