using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        static Settings() => LoadSettings(ref s_instance);

#region Fields

        [field: NonSerialized]
        private const string s_settingsFileName = @".\settings.config";

        [field: NonSerialized]
        private const string s_globDlls = @"**\*.dll";

        [field: NonSerialized]
        private static readonly Settings s_instance = null!;

        [field: NonSerialized]
        private static readonly Lazy<Settings> s_default = new Lazy<Settings>(() => new Settings {FrameworkAssemblyPaths = new[] {@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App", @"C:\Windows\Microsoft.NET\Framework"}, FrameworkAssemblyNameHashes = new[] {-930347654, -400742716, -221722442, -137426065, -24218821, -13723083, -7499274, 2799317, 9885587, 14629153, 19285092, 25267538, 25858742, 26488117, 35129861, 36002154, 36825297, 37360611, 44081095, 45968029, 48513270, 53541389, 58053447, 61024954, 71187233, 72424988, 76451044, 76588293, 77607150, 77630615, 81477497, 81477497, 86903396, 91313636, 95253169, 98271587, 100005818, 101402446, 105540109, 105865525, 109399181, 111854175, 113303145, 125106116, 131724618, 132339474, 134560067, 134840267, 135268455, 138045464, 138474917, 148424711, 148632153, 149946166, 150783828, 150941992, 153007164, 153334581, 154528314, 158922791, 160486379, 164044492, 167599568, 167665773, 170629904, 174370326, 174954235, 176332918, 178953703, 179918902, 181301709, 181877117, 182559886, 183153711, 188156494, 191764358, 199527465, 202867509, 204518756, 207678579, 208762221, 220945161, 222335717, 231285043, 233264862, 238621143, 239680104, 240388190, 246682217, 249763471, 250973880, 252905551, 254731019, 268000251, 276424962, 280627780, 281674743, 282033090, 292886455, 294727778, 303714215, 303872724, 308123563, 309087537, 312427602, 314963117, 316094658, 320792261, 321492148, 328085396, 332513365, 333552229, 340976705, 348846907, 351420029, 351420029, 353067161, 357717273, 358369094, 360367060, 361218542, 367120813, 368485040, 368910575, 373384324, 377411441, 377411441, 381060473, 385712786, 386711677, 386772899, 387103380, 388999838, 389135155, 392447391, 400595361, 401718141, 402321771, 406780920, 407181528, 411611652, 415232641, 416407606, 417381346, 427737729, 431288464, 432039375, 433435761, 440470832, 440830019, 442258563, 449960529, 450969639, 457162214, 458480204, 458512429, 461149874, 463625279, 464724564, 467919901, 468551592, 472313741, 472822718, 474257279, 475850583, 478697785, 488562070, 491340903, 494499735, 495612746, 495612746, 497221314, 498145448, 498145448, 501357647, 502032585, 504277163, 516157643, 516545908, 517643713, 518726437, 521245868, 521470778, 521963310, 522173453, 524337068, 526138487, 530501780, 537253643, 539022306, 543349856, 545712971, 552623815, 553861997, 559575260, 565081970, 568160317, 573900682, 574937069, 579428066, 580281579, 584579670, 590295929, 590584636, 598121343, 598619106, 602986811, 605965983, 611529551, 612046966, 620696617, 621726826, 623375384, 625965568, 626934841, 631353605, 633050530, 633989898, 635311465, 636219476, 636235324, 639332015, 644645209, 653849163, 656241773, 661771446, 665591073, 665835793, 672376200, 673363648, 673366858, 673560673, 676386315, 681220187, 681325045, 682283823, 701493978, 704238484, 712634189, 715610601, 716334589, 716911614, 727247081, 732331242, 735023875, 735216047, 741634778, 742610994, 745655873, 751747097, 756684681, 757676002, 758707898, 761536944, 770513921, 771497466, 775234561, 775568366, 779701678, 784649239, 786760603, 789764594, 798680272, 798756910, 806927237, 816025231, 816573539, 820799898, 821344296, 822100944, 832297242, 840095347, 843859492, 844519975, 854524469, 858056472, 859378609, 862287626, 866431314, 867142252, 872220742, 876578880, 880107100, 884928706, 886383446, 889812908, 892034487, 892619385, 895825001, 898231435, 902225868, 909147334, 911503421, 916036679, 928222877, 933772906, 935707123, 940295783, 944744614, 949210196, 952340342, 954454832, 955269497, 955737095, 955749374, 957690249, 965082844, 965680153, 969356264, 971074584, 975519421, 978732698, 981199041, 983733591, 995291958, 998207833, 999180881}});

        private string[] _frameworkAssemblyPaths = null!;

        private int[] _frameworkAssemblyNameHashes = null!;

        [field: NonSerialized]
        private bool _synchronizeSettings;

        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

#endregion


#region Properties

        [field: NonSerialized]
        public Task WriteTask { get; private set; } = Task.CompletedTask;

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
            set => this.Set(ref this._frameworkAssemblyNameHashes, value);
        }

#endregion

#region Public members

        public void DisableSettingsSynchronization()
        {
            if (!this._synchronizeSettings)
            {
                return;
            }

            this.WriteTask.Wait();
            this._synchronizeSettings = false;
        }

        public void EnableSettingsSynchronization()
        {
            if (this._synchronizeSettings)
            {
                return;
            }

            this._synchronizeSettings = true;
            this.WriteTask = Task.Run(StoreSettings);
        }

        public Settings Clone() => new Settings {FrameworkAssemblyPaths = this.FrameworkAssemblyPaths, FrameworkAssemblyNameHashes = this.FrameworkAssemblyNameHashes};

        object ICloneable.Clone() => this.Clone();

#endregion

#region Private members

        private static void LoadSettings(ref Settings obj)
        {
            if (!File.Exists(s_settingsFileName))
            {
                goto ReturnDefault;
            }

            Settings? deserialized = null;
            try
            {
                using var fs = new FileStream(s_settingsFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                using var xr = new XmlTextReader(sr);
                var serializer = new XmlSerializer(typeof(Settings));
                deserialized = serializer.Deserialize(xr) as Settings;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Could not open settings from file {s_settingsFileName}. " + ex.ToLog(true));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Could not reflect settings object from file {s_settingsFileName}. " + ex.ToLog(true));
            }

            if (deserialized is null)
            {
                goto ReturnDefault;
            }

            deserialized.FrameworkAssemblyNameHashes = AssemblyHashDumper.AssemblyNameHashes(
                    deserialized
                       .FrameworkAssemblyPaths
                       .Where(Directory.Exists)
                       .SelectMany(d => Glob.Files(d, s_globDlls)),
                    Console.WriteLine)
               .ToArray();
            obj = deserialized;
            obj.EnableSettingsSynchronization();

            ReturnDefault:
            obj = Default.Clone();
            obj.EnableSettingsSynchronization();
        }

        private static void StoreSettings()
        {
            try
            {
                using var fs = new FileStream(s_settingsFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using var xw = new XmlTextWriter(fs, Encoding.UTF8);
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(xw, Instance);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Could not write settings. {s_settingsFileName}", ex);
            }
        }


        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            if (this._synchronizeSettings)
            {
                this.WriteTask.Wait();
            }

            field = value;

            if (this._synchronizeSettings)
            {
                this.WriteTask = Task.Run(StoreSettings);
            }

            if (propertyName != null)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

#endregion
    }
}
