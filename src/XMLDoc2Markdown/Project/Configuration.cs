using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using XMLDoc2Markdown.Extensions;

namespace XMLDoc2Markdown.Project
{
    public static class Configuration
    {
        private const string s_xmlSchemaFileName = @".\ProjectSChema.xsd";
        private static Project? s_current;

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

        public static bool IsLoaded => s_current != null;

        public static Project? Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The file could not be found. " + fileName);
            }

            try
            {
                using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);
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
                XmlSerializer xs = new XmlSerializer(typeof(Project));
                s_current = xs.Deserialize(sr) as Project;
                Validate(s_current);
                return s_current;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Could not open the file. " + fileName + ex.ToLog(true));
            }
        }

        public static void Unload() => s_current = null;

        public static void Store(string fileName)
        {
            if (s_current is null)
            {
                throw new InvalidOperationException("No configuration loaded. Load a configuration first.");
            }

            lock (s_current)
            {
                using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                using var sr = new StreamWriter(fs, Encoding.UTF8);
                XmlSerializer xs = new XmlSerializer(typeof(Project));
                xs.Serialize(sr, s_current);
            }
        }

        public static Project Create()
        {
            if (s_current != null)
            {
                throw new InvalidOperationException("A configuration is already loaded. Unload the configuration first.");
            }

            s_current = new Project();
            return s_current;
        }

        /// <summary>Additional validation, such as checking whether files exist. Throws XmlSchemaValidationException</summary>
        private static void Validate(Project? project)
        {
            // Initialized
            if (project is null || project.Properties is null || project.Assembly is null)
            {
                throw new XmlSchemaValidationException("Project is not initialized correctly.");
            }

            // Properties
            if (!string.IsNullOrWhiteSpace(project.Properties.NamespaceMatch) && !project.Properties.NamespaceMatch.IsValidRegex() && !project.Properties.NamespaceMatch.IsGlobExpression())
            {
                throw new XmlSchemaValidationException("Namespace is an invalid regex and glob.");
            }

            if (project.Properties.Index is null || string.IsNullOrWhiteSpace(project.Properties.Index.Name))
            {
                project.Properties.Index = new Index {Name = "index"};
            }

            if (string.IsNullOrWhiteSpace(project.Properties.Output.Path))
            {
                throw new XmlSchemaValidationException("Output path is null or whitespace.");
            }

            // Assembly
            if (project.Assembly.Length == 0)
            {
                throw new XmlSchemaValidationException("No assemblies are defined in the project.");
            }

            for (int i = 0; i < project.Assembly.Length; i++)
            {
                Assembly? assembly = project.Assembly[i];
                if (assembly is null)
                {
                    throw new XmlSchemaValidationException("The assembly #{i} is not initialized correctly.");
                }

                if (!File.Exists(assembly.File))
                {
                    throw new XmlSchemaValidationException($"Invalid filename for assembly #{i}. {assembly.File}");
                }

                if (assembly.IndexHeader != null && assembly.IndexHeader.File != null && assembly.IndexHeader.Text != null && assembly.IndexHeader.Text.Length != 0)
                {
                    throw new XmlSchemaValidationException($"Invalid <IndexHeader>-tag for assembly #{i}. Cant have both a file and raw text defined.");
                }

                if (assembly.References != null)
                {
                    int j = 0;
                    foreach (string reference in assembly.References.AssemblyReference.Concat(assembly.References.NugetReference))
                    {
                        if (!File.Exists(reference))
                        {
                            throw new XmlSchemaValidationException($"Invalid filename for reference #{j} in assembly #{i}. {reference}");
                        }

                        j++;
                    }
                }
            }
        }
    }
}
