using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;

using Microsoft.Extensions.CommandLineUtils;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Project;

using Assembly = System.Reflection.Assembly;
using Index = XMLDoc2Markdown.Project.Index;

namespace XMLDoc2Markdown
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
#if DEBUG
            Debugger.Launch();
            string? solutionRoot = Environment.CurrentDirectory;
            DirectoryInfo? parent = null;
            while (!(solutionRoot is null) && !string.Equals(parent?.Name, "xmldoc2md", StringComparison.InvariantCultureIgnoreCase))
            {
                parent = new DirectoryInfo(solutionRoot).Parent;
                solutionRoot = parent?.FullName;
            }

            if (solutionRoot is null)
            {
                throw new Exception();
            }

            args = new[] { Path.Combine(solutionRoot, @"publish\MyClassLib.dll"), "-o", Path.Combine(solutionRoot, @"docs\sample")};
            //args = new[] {Path.Combine(solutionRoot, "sample\\sample.x2mproj")};

#endif
            var app = new CommandLineApplication {Name = "xmldoc2md"};

            app.VersionOption("-v|--version", () => $"Version {Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
            app.HelpOption("-?|-h|--help");

            CommandArgument srcArg = app.Argument("src", 
                "File path or file glob specifying the assembly/ies to generate the XML documentation for.\r\n"
              + "Or the file path of the project file to execute.");

            CommandOption outOption = app.Option(
                "-o|--output;lt;output_directory&amp;gt;",
                "Directory path to the output directory of the documentation. \r\n"
              + "Must be specified when generating from source. But overwrites the output when executing a project file.",
                CommandOptionType.SingleValue);

            CommandOption namespaceMatchOption = app.Option(
                "-n|--namespace-match;lt;regex_or_glob&amp;gt;",
                "Glob or regex pattern to select namespaces",
                CommandOptionType.SingleValue);

            CommandOption indexPageNameOption = app.Option(
                "--index-page-name <regex>",
                "Name of the index page (default: \"index\")",
                CommandOptionType.SingleValue);

            app.OnExecute(
                () =>
                {
                    string? @out = outOption.HasValue() ? outOption.Value() : null;
                    string? src = srcArg.Value;
                    string? namespaceMatch = namespaceMatchOption.Value();
                    string indexPageName = indexPageNameOption.HasValue() ? indexPageNameOption.Value() : "index";
                    
                    if (src is null)
                    {
                        throw new CommandParsingException(app, "src is undefined.");
                    }
                    

                    // Ensure that settings have loaded
                    Settings.Instance.WriteTask.Wait();

                    // Project file
                    if (Path.GetExtension(src) == ".x2mproj")
                    {
                        return ConfigureFromProject(src, @out);
                    }

                    // Assembly files
                    if (@out is null)
                    {
                        throw new CommandParsingException(app, "out must be defined, when not executing a project file.");
                    }

                    EnsureDirectory(@out);

                    return ConfigureFromFile(src!, @out, namespaceMatch, indexPageName);
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
            Project.Project project = Configuration.Create(Path.Combine(Environment.CurrentDirectory, "__dummy.x2mproj"));

            project.Properties = new Properties {Index = new Index {Name = indexPageName}, NamespaceMatch = namespaceMatch, Output = new Output {Path = @out}};

            project.Assembly = new Project.Assembly[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                project.Assembly[i] = new Project.Assembly {Documentation = null, File = targets[i], IndexHeader = new IndexHeader {File = null, Text = null}, References = null};
            }

            DocumentationProcessor.WriteCurrentProjectConfiguration();
            return 0;
        }

        private static int ConfigureFromProject(string projectFilePath, string? @out)
        {
            if (!File.Exists(projectFilePath))
            {
                Console.WriteLine("The specified project file could not be found. " + projectFilePath);
                return -2;
            }

            // Prepare project configuration
            try
            {
                Configuration.Load(projectFilePath);
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

            if (!Configuration.IsLoaded)
            {
                Console.WriteLine("The project could no be loaded correctly.");
                return -1;
            }

            // Overwrite the output path, if specified
            if (!string.IsNullOrWhiteSpace(@out))
            {
                EnsureDirectory(@out);
                Configuration.Current.Properties.Output.Path = @out;
            }
            
            DocumentationProcessor.WriteCurrentProjectConfiguration();
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
