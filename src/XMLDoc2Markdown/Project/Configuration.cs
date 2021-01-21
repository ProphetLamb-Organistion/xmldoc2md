using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace XMLDoc2Markdown.Project
{
    public static class Configuration
    {
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
            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            XmlSerializer xs = new XmlSerializer(typeof(Project));
            s_current = xs.Deserialize(sr) as Project;
            return s_current;
        }

        public static void Unload() => s_current = null;

        public static void Store(string fileName)
        {
            if (s_current is null)
            {
                throw  new InvalidOperationException("No configuration loaded. Load a configuration first.");
            }

            lock (s_current)
            {
                using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                using var sr = new StreamWriter(fs, Encoding.UTF8);
                XmlSerializer xs = new XmlSerializer(typeof(Project));
                xs.Serialize(sr, s_current);
            }
        }
    }
}
