using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using GlobExpressions;

namespace XMLDoc2Markdown.Extensions
{
    [Flags]
    public enum FileType
    {
        FileOrDirectory = 3,
        File = 1,
        Directory = 2
    }

    internal static class StringExtensions
    {
        private static readonly string[] s_formatChevrons_oldValues = {"<", ">"};
        private static readonly string[] s_formatChevrons_newValues = {"&lt;", "&gt;"};

        private static readonly string[] s_typeNameToFileName_oldValues = {"<", ">", ",", " ", "`"};
        private static readonly string[] s_typeNameToFileName_newValues = {"{", "}", "", "-", "-"};

        private static readonly char[] s_globWildcards = {'*', '?', '['};
        private static readonly char[] s_pathSeparators = {'\\', '/'};

        public static string FormatChevrons(this string value) => value.ReplaceMany(s_formatChevrons_oldValues, s_formatChevrons_newValues);

        public static string MakeTypeNameFileNameSafe(string typeName) => typeName.ReplaceMany(s_typeNameToFileName_oldValues, s_typeNameToFileName_newValues);

        public static bool IsValidRegex(this string self)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                return false;
            }

            try
            {
                _ = Regex.Match("", self);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public static bool IsGlobExpression(this string self)
        {
            if (string.IsNullOrWhiteSpace(self))
            {
                return false;
            }

            if (self.IndexOfAny(s_globWildcards) == -1)
            {
                return false;
            }

            try
            {
                _ = new Glob(self).IsMatch("test");
            }
            catch (GlobPatternException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the full file path and names of all files, that fulfill the glob pattern.
        /// </summary>
        /// <param name="globFilePath">The glob file pattern.</param>
        /// <param name="type">The type of the file target.</param>
        /// <returns>An enumerable sequence of full file path and names that fulfill the glob pattern.</returns>
        public static IEnumerable<string> GetGlobFiles(this string globFilePath, FileType type = FileType.File)
        {
            IEnumerable<string> results = GetGlobFiles(globFilePath, out string? directory, type);
            if (directory is null)
            {
                return Enumerable.Empty<string>();
            }

            return results.Select(f => Path.Combine(directory, f));
        }

        /// <summary>
        /// Returns the file path and names relative to the shared root, of all files, that fulfill the glob pattern.
        /// </summary>
        /// <param name="globFilePath">The glob file pattern.</param>
        /// <param name="sharedRootDirectory">The directory that all files that fulfill the glob pattern share.</param>
        /// <para name="type">The type of the file target.</para>
        /// <returns>An enumerable sequence of file path and names relative to the shared root, of all files, that fulfill the glob pattern.</returns>
        public static IEnumerable<string> GetGlobFiles(this string globFilePath, out string? sharedRootDirectory, FileType type = FileType.File)
        {
            if (!Path.IsPathRooted(globFilePath))
            {
                sharedRootDirectory = Environment.CurrentDirectory;
                return type switch
                {
                    FileType.File => Glob.Files(Environment.CurrentDirectory, globFilePath),
                    FileType.Directory => Glob.Directories(Environment.CurrentDirectory, globFilePath),
                    FileType.FileOrDirectory => Glob.FilesAndDirectories(Environment.CurrentDirectory, globFilePath),
                    _ => throw new ArgumentOutOfRangeException(nameof(type))
                };
            }

            // Rooted path, begins with C: etc.
            if (globFilePath.IndexOfAny(s_globWildcards) == -1)
            {
                if ((type & FileType.File) != 0)
                {
                    var fi = new FileInfo(globFilePath);
                    if (fi.Exists)
                    {
                        sharedRootDirectory = fi.DirectoryName!;
                        return new [] {fi.Name};
                    }
                }
                if ((type & FileType.Directory) != 0)
                {
                    var di = new DirectoryInfo(globFilePath);
                    if (di.Exists)
                    {
                        sharedRootDirectory = di.Parent!.FullName;
                        return new [] {di.Name};
                    }
                }

                sharedRootDirectory = null;
                return Enumerable.Empty<string>();
            }

            // Split the glob as close to wildcard as possible
            string[] pathPortions = globFilePath.Split(s_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            int portionBreakIndex = pathPortions.TakeWhile(portion => portion.IndexOfAny(s_globWildcards) == -1).Count();

            sharedRootDirectory = string.Join('\\', pathPortions, 0, portionBreakIndex);
            string globPortion = portionBreakIndex == pathPortions.Length ? string.Empty : string.Join('\\', pathPortions, portionBreakIndex, pathPortions.Length - portionBreakIndex);
            
            return type switch
            {
                FileType.File => Glob.Files(sharedRootDirectory, globPortion),
                FileType.Directory => Glob.Directories(sharedRootDirectory, globPortion),
                FileType.FileOrDirectory => Glob.FilesAndDirectories(sharedRootDirectory, globPortion),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static string ReplaceMany(this string self, ICollection<string> oldValues, ICollection<string> newValues)
        {
            if (oldValues.Count != newValues.Count)
            {
                throw new ArgumentException("oldValues.Count != newValues.Count");
            }

            var sb = new StringBuilder(self);
            using IEnumerator<string> oen = oldValues.GetEnumerator();
            using IEnumerator<string> nen = newValues.GetEnumerator();
            while (oen.MoveNext() && nen.MoveNext())
            {
                if (oen.Current is null)
                {
                    throw new InvalidOperationException("oldValues cannot contain null strings.");
                }

                sb.Replace(oen.Current, nen.Current);
            }

            return sb.ToString();
        }
    }
}
