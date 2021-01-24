using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;

using GlobExpressions;

using Microsoft.Extensions.CommandLineUtils;

using XMLDoc2Markdown.Extensions;

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

            //args = new[] { Path.Combine(solutionRoot, @"publish\MyClassLib.dll"), "-o", Path.Combine(solutionRoot, @"docs\sample")};
            //args = new[] {Path.Combine(solutionRoot, "sample\\sample.x2mproj")};
            //args = new[] {"new", @"C:\Users\Public\source\repos\Groundbeef\src\**", @"bin\x64\Release\{netcoreapp3.1,netstandard2.1}\*{Project}.dll",  @"C:\Users\Public\source\repos\Groundbeef\docproject.x2mproj", "-o", @"C:\Users\Public\source\repos\Groundbeef\docproject"};
            args = new[] { @"C:\Users\Public\source\repos\Groundbeef\docproject.x2mproj" };

#endif
            var app = new CommandLineApplication {Name = "xmldoc2md"};

            app.Command("new", NewCommand);

            app.VersionOption("-v|--version", () => $"Version {Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
            app.HelpOption("-?|-h|--help");

            CommandArgument srcArg = app.Argument(
                "src", 
                "File path or file glob specifying the assembly/ies to generate the XML documentation for.\r\n"
              + "Or the file path of the project file to execute.");

            CommandOption outOption = app.Option(
                "-o|--output <output_directory>",
                "Directory path to the output directory of the documentation. \r\n"
              + "Must be specified when generating from source. But overwrites the output when executing a project file.",
                CommandOptionType.SingleValue);
            CommandOption namespaceMatchOption = app.Option(
                "-n|--namespace-match <regex_or_glob>",
                "Glob or regex pattern to select namespaces",
                CommandOptionType.SingleValue);
            CommandOption indexOption = app.Option(
                "--index <regex>",
                "Name of the index page (default: \"index\")",
                CommandOptionType.SingleValue);

            app.OnExecute(
                () =>
                {
                    string? @out = outOption.HasValue() ? outOption.Value() : null;
                    string? src = srcArg.Value;
                    string? namespaceMatch = namespaceMatchOption.Value();
                    string indexPageName = indexOption.HasValue() ? indexOption.Value() : "index";
                    
                    if (src is null)
                    {
                        throw new CommandParsingException(app, "src is undefined.");
                    }

                    Settings.Initialize();
                    
                    if (Path.GetExtension(src) == ".x2mproj")
                    {
                        if (!File.Exists(src))
                        {
                            Console.WriteLine("The specified project file could not be found. " + src);
                            return -2;
                        }

                        // Prepare project configuration
                        try
                        {
                            Project.Configuration.Load(src);
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

                        // Overwrite the output path, if specified
                        if (!string.IsNullOrWhiteSpace(@out))
                        {
                            EnsureDirectory(@out);
                            Project.Configuration.Current.Properties.Output.Path = @out;
                        }
            
                        DocumentationProcessor.WriteCurrentProjectConfiguration();
                    }
                    else
                    {
                        if (@out is null)
                        {
                            throw new CommandParsingException(app, "out must be defined, when not executing a project file.");
                        }

                        EnsureDirectory(@out);
                    
                        string[] targets = src.GetGlobFiles().ToArray();

                        // Generate project configuration
                        Project.Project project = Project.Configuration.Create(Path.Combine(Environment.CurrentDirectory, "__dummy.x2mproj"));

                        project.Properties = new Project.Properties {Index = new Project.Index {Name = indexPageName}, NamespaceMatch = namespaceMatch, Output = new Project.Output {Path = @out}};

                        project.Assembly = new Project.Assembly[targets.Length];
                        for (int i = 0; i < targets.Length; i++)
                        {
                            project.Assembly[i] = new Project.Assembly {Documentation = null, File = targets[i], IndexHeader = new Project.IndexHeader {File = null, Text = null}, References = null};
                        }

                        DocumentationProcessor.WriteCurrentProjectConfiguration();
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
                Console.WriteLine("Unable to execute application." + ex.ToLog());
            }
        }

        public static void NewCommand(CommandLineApplication app)
        {
            app.VersionOption("-v|--version", () => $"Version {Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
            app.HelpOption("-?|-h|--help");

            CommandArgument projectArg = app.Argument(
                "project",
                "Path or glob specifying all Visual Studio projects roots to generate the project file for. Directory specified must contain exactly one *.csproj file.");
            CommandArgument fileArg = app.Argument(
                "file",
                "The relative path from the project root to the *.dll-file containing the assembly associated with the given project. {Project} is a symbol for the name of the project.");
            CommandArgument outArg = app.Argument(
                "out",
                "File path of the project file to generate. Ends with .x2mproj.");

            CommandOption indexOption = app.Option(
                "--index <regex>",
                "Name of the index page (default: \"index\")",
                CommandOptionType.SingleValue);
            CommandOption namespaceMatchOption = app.Option(
                "-n|--namespace-match <regex_or_glob>",
                "Glob or regex pattern to select namespaces",
                CommandOptionType.SingleValue);
            CommandOption outOption = app.Option(
                "-o|--output <output_directory>",
                "Directory path to the output directory of the documentation.",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                string? src = projectArg.Value;
                string? file = fileArg.Value;
                string? @out = outArg.Value;
                string? indexName = indexOption.HasValue() ? indexOption.Value() : null;
                string? namespaceMatch = namespaceMatchOption.HasValue() ? namespaceMatchOption.Value() : null;
                string? output = outOption.HasValue() ? outOption.Value() : null;
                    
                if (src is null)
                {
                    throw new CommandParsingException(app, "src is undefined.");
                }
                if (Path.GetExtension(src) != ".csproj")
                {
                    src = Path.Combine(src!, "*.csproj");
                }

                if (file is null)
                {
                    throw new CommandParsingException(app, "file is undefined.");
                }

                if (@out is null)
                {
                    throw new CommandParsingException(app, "out must be defined.");
                }
                if (Path.GetExtension(@out) != ".x2mproj")
                {
                    @out += ".x2mproj";
                }

                if (output is null)
                {
                    throw new CommandParsingException(app, "output must be defined.");
                }

                var generated = new Project.Project();
                string[] projectDirectories = src.GetGlobFiles().Select(f => Path.GetDirectoryName(f)!).ToArray();

                generated.Properties = new Project.Properties
                {
                    Index = new Project.Index {Name = indexName},
                    Output = new Project.Output {Path = @out},
                    NamespaceMatch = namespaceMatch
                };

                var assembies = new List<Project.Assembly>();
                foreach (string project in projectDirectories)
                {
                    Project.Assembly assembly = new Project.Assembly();
                    string? assemblyFileName = Glob.Files(project, file.Replace(@"{Project}", Path.GetFileName(project), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (assemblyFileName is null)
                    {
                        Console.WriteLine($"The assembly '{file}' could not be found in the project '{project}'.");
                        continue;
                    }
                    assemblyFileName = Path.Combine(project, assemblyFileName);

                    assembly.File = assemblyFileName;
                    assembly.Documentation = Path.Combine(Path.GetDirectoryName(assemblyFileName)!, Path.GetFileNameWithoutExtension(assemblyFileName) + ".xml");
                    assembly.References = new Project.References();
                    assembly.IndexHeader = new Project.IndexHeader();

                    assembies.Add(assembly);
                }
                generated.Assembly = assembies.ToArray();

                Project.Configuration.Load(generated, output);
                Project.Configuration.Store();
                return 0;
            });
        }

        private static void EnsureDirectory(string? directoryName)
        {
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName!);
            }
        }
    }
}
