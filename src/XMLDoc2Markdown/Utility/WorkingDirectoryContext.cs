using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XMLDoc2Markdown.Utility
{
    public class WorkingDirectoryContext : IDisposable
    {
        private string? _primaryWorkingDirectory;

        public WorkingDirectoryContext(string tempoaryWorkingDirectory)
        {
            if (!Directory.Exists(tempoaryWorkingDirectory))
            {
                throw new DirectoryNotFoundException($"The temporary working directory does not exist. '{tempoaryWorkingDirectory}'");
            }
            this._primaryWorkingDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempoaryWorkingDirectory;
        }

        public void Dispose()
        {
            if (this._primaryWorkingDirectory is null)
            {
                return;
            }

            Environment.CurrentDirectory = this._primaryWorkingDirectory;
            this._primaryWorkingDirectory = null;
        }

        public static IDisposable? Modulate(string? tempraryWorkingDirectory)
        {
            // Change working directory to project directory
            string? newWorkingDirectory = tempraryWorkingDirectory;
            if (!Path.IsPathRooted(newWorkingDirectory))
            {
                newWorkingDirectory = Path.GetFullPath(newWorkingDirectory!);
            }

            if (!Directory.Exists(newWorkingDirectory))
            {
                return null;
            }

            return new WorkingDirectoryContext(newWorkingDirectory!);
        }
    }
}
