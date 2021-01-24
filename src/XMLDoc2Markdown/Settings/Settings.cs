using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using GlobExpressions;

using XMLDoc2Markdown.Extensions;
using XMLDoc2Markdown.Utility;

namespace XMLDoc2Markdown
{
    [Serializable]
    public sealed class Settings : INotifyPropertyChanged, ICloneable
    {
#region Fields
        private static ReaderWriterLockSlim s_wLock = new ReaderWriterLockSlim();

        private const string s_settingsFileName = @".\settings.config";
        private const string s_globDlls = @"**\*.dll";

        private static readonly Settings s_instance = null!;
        private static readonly Lazy<Settings> s_default = new Lazy<Settings>(() => new Settings
        {
            FrameworkAssemblyPaths = new[]
            {
                @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App", @"C:\Windows\Microsoft.NET\Framework", @"C:\Windows\Microsoft.NET\Framework64"
            }, 
            FrameworkAssemblyNameHashes = new int[0]
        });

        private string[] _frameworkAssemblyPaths = null!;
        private int[] _frameworkAssemblyNameHashes = null!;
        private bool _synchronizeSettings;
        private Task _writeTask = Task.CompletedTask;

        public event PropertyChangedEventHandler? PropertyChanged;

#endregion

        static Settings()
        {
            LoadSettings(ref s_instance);
        }


#region Properties
        
        public static Settings Instance => s_instance;

        public static Settings Default => s_default.Value;

        [XmlArray]
        public string[] FrameworkAssemblyPaths
        {
            get => this._frameworkAssemblyPaths;
            set => this.Set(ref this._frameworkAssemblyPaths, value);
        }

        [XmlArray]
        public int[] FrameworkAssemblyNameHashes
        {
            get => this._frameworkAssemblyNameHashes;
            set
            {
                Array.Sort(value);
                this.Set(ref this._frameworkAssemblyNameHashes, value);
            }
        }

#endregion

#region Public members

        public static void Initialize()
        {
            // Force calling the static ctor
            if (s_instance is null)
            {
                throw new InvalidOperationException("Initialization failed.");
            }
        }

        public void DisableSettingsSynchronization()
        {
            if (!this._synchronizeSettings)
            {
                return;
            }

            this._writeTask.Wait();
            this._synchronizeSettings = false;
        }

        public void EnableSettingsSynchronization()
        {
            if (this._synchronizeSettings)
            {
                return;
            }

            this._synchronizeSettings = true;
            this._writeTask = Task.Run(StoreSettings);
        }

        public Settings Clone() => new Settings {FrameworkAssemblyPaths = this.FrameworkAssemblyPaths, FrameworkAssemblyNameHashes = this.FrameworkAssemblyNameHashes};

        object ICloneable.Clone() => this.Clone();

#endregion

#region Private members

        private static void LoadSettings(ref Settings obj)
        {
            if (!File.Exists(s_settingsFileName))
            {
                obj = Default.Clone();
                goto HashAndReturn;
            }

            Settings? deserialized = null;
            try
            {
                using var sr = new StreamReader(File.Open(s_settingsFileName, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8);
                using var xr = new XmlTextReader(sr);
                var serializer = new XmlSerializer(typeof(Settings));
                deserialized = serializer.Deserialize(xr) as Settings;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Could not open settings from file '{s_settingsFileName}'. " + ex.ToLog(true));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Could not reflect settings object from file '{s_settingsFileName}'. " + ex.ToLog(true));
            }

            if (deserialized is null)
            {
                obj = Default.Clone();
                goto HashAndReturn;
            }

            obj = deserialized;

            HashAndReturn:
            if (obj.FrameworkAssemblyNameHashes is null || obj.FrameworkAssemblyNameHashes.Length == 0)
            {
                obj.FrameworkAssemblyNameHashes = AssemblyHashDumper.AssemblyNameHashes(
                        obj
                           .FrameworkAssemblyPaths
                           .Where(Directory.Exists)
                           .SelectMany(d => Glob.Files(d, s_globDlls).Select(f => Path.Combine(d, f))),
                        Console.WriteLine)
                   .ToArray();
            }
            obj.EnableSettingsSynchronization();
        }

        private static void StoreSettings()
        {
            s_wLock.EnterWriteLock();
            try
            {
                using var xw = new XmlTextWriter(File.Open(s_settingsFileName, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(xw, Instance);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Could not write settings. '{s_settingsFileName}'", ex);
            }
            finally
            {
                s_wLock.ExitWriteLock();
            }
        }


        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;

            if (this._synchronizeSettings)
            {
                this._writeTask = Task.Run(StoreSettings);
            }

            if (propertyName != null)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

#endregion
    }
}
