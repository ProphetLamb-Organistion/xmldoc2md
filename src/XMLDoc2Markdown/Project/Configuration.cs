using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown.Project
{
    public static class Configuration
    {
        private const string s_xmlSchemaFileName = @".\ProjectSchema.xsd";
        private static Project? s_current;
        private static string? s_filePath;

        public static Project Current
        {
            get
            {
                if (s_current is null)
                {
                    throw new InvalidOperationException("No configuration loaded. Load a configuration first.");
                }

                return s_current;
            }
        }

        public static string? ProjectFilePath
        {
            get
            {
                if (s_current is null)
                {
                    throw new InvalidOperationException("No configuration loaded. Load a configuration first.");
                }

                return s_filePath;
            }
        }

        public static bool IsLoaded => s_current != null;

        public static Project? Load(Project project, string filePath)
        {
            if (s_current != null)
            {
                throw new InvalidOperationException("A configuration is already loaded. Unload the configuration first.");
            }

            s_current = project;
            s_filePath = filePath;

            ValidateCurrent();

            return s_current;
        }

        public static Project? Load(string filePath)
        {
            if (s_current != null)
            {
                throw new InvalidOperationException("A configuration is already loaded. Unload the configuration first.");
            }
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("The file could not be found. " + filePath);
            }

            try
            {
                using var sr = new StreamReader(File.OpenRead(filePath), Encoding.UTF8);

                // Validate schema
                XmlSchemaSet schema = new XmlSchemaSet();
                schema.Add(string.Empty, s_xmlSchemaFileName);
                XmlReader xr = XmlReader.Create(sr);
                XDocument temp = XDocument.Load(xr);
                temp.Validate(
                    schema,
                    (sender, args) =>
                    {
                        if (args.Severity == XmlSeverityType.Error)
                        {
                            throw args.Exception;
                        }
                    });

                // Generate project and validate data 
                XmlSerializer xs = new XmlSerializer(typeof(Project));
                s_current = xs.Deserialize(temp.CreateReader()) as Project;
                s_filePath = filePath;

                ValidateCurrent();

                return s_current;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Could not open the file. " + filePath + ex.ToLog(true));
            }
        }

        public static void Unload()
        {
            s_current = null;
            s_filePath = null;
        }

        public static void Store()
        {
            if (s_current is null)
            {
                throw new InvalidOperationException("No configuration loaded. Load a configuration first.");
            }

            lock (s_current)
            {
                using var sr = new StreamWriter(File.OpenWrite(s_filePath!), Encoding.UTF8);
                XmlSerializer xs = new XmlSerializer(typeof(Project));
                xs.Serialize(sr, s_current);
            }
        }

        public static Project Create(string? projectFilePath = null)
        {
            if (s_current != null)
            {
                throw new InvalidOperationException("A configuration is already loaded. Unload the configuration first.");
            }

            if (projectFilePath != null)
            {
                s_filePath = projectFilePath;
            }

            s_current = new Project();
            return s_current;
        }

        /// <summary>Additional validation, such as checking whether files exist. Throws XmlSchemaValidationException</summary>
        private static void ValidateCurrent()
        {
            // Initialized
            if (s_current is null || s_current.Properties is null || s_current.Assembly is null)
            {
                throw new XmlSchemaValidationException("Project is not initialized correctly.");
            }

            // Properties
            if (!string.IsNullOrWhiteSpace(s_current.Properties.NamespaceMatch) && !s_current.Properties.NamespaceMatch.IsValidRegex() && !s_current.Properties.NamespaceMatch.IsGlobExpression())
            {
                throw new XmlSchemaValidationException("Namespace is an invalid regex and glob.");
            }

            if (s_current.Properties.Index is null || string.IsNullOrWhiteSpace(s_current.Properties.Index.Name))
            {
                s_current.Properties.Index = new Index {Name = "index"};
            }

            if (string.IsNullOrWhiteSpace(s_current.Properties.Output.Path))
            {
                throw new XmlSchemaValidationException("Output path is null or whitespace.");
            }

            // Assembly
            if (s_current.Assembly.Length == 0)
            {
                throw new XmlSchemaValidationException("No assemblies are defined in the s_current.");
            }

            for (int i = 0; i < s_current.Assembly.Length; i++)
            {
                Assembly? assembly = s_current.Assembly[i];

                if (assembly is null)
                {
                    throw new XmlSchemaValidationException("The assembly #{i} is not initialized correctly.");
                }

                using (WorkingDirectoryContext.Modulate(Path.GetDirectoryName(s_filePath)))
                {
                    if (!File.Exists(assembly.File))
                    {
                        throw new XmlSchemaValidationException($"Invalid filename for assembly #{i}. '{assembly.File}'");
                    }

                    if (assembly.IndexHeader != null && assembly.IndexHeader.File != null && assembly.IndexHeader.Text != null && assembly.IndexHeader.Text.Length != 0)
                    {
                        throw new XmlSchemaValidationException($"Invalid <IndexHeader>-tag for assembly #{i}. Cant have both a file and raw text defined.");
                    }

                    if (assembly.References != null)
                    {
                        if (assembly.References.AssemblyReference != null)
                        {
                            for (int j = 0; j < assembly.References.AssemblyReference.Length; j++)
                            {
                                string reference = assembly.References.AssemblyReference[j];
                                if (!File.Exists(reference))
                                {
                                    throw new XmlSchemaValidationException($"Invalid filename for assembly-reference #{j} in assembly #{i}. '{reference}'");
                                }
                            }
                        }
                        if (assembly.References.NugetReference != null)
                        {
                            for (int j = 0; j < assembly.References.NugetReference.Length; j++)
                            {
                                string reference = assembly.References.NugetReference[j];
                                if (!File.Exists(reference))
                                {
                                    throw new XmlSchemaValidationException($"Invalid filename for nuget-reference #{j} in assembly #{i}. '{reference}'");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
