using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using GlobExpressions;

namespace XMLDoc2Markdown.Extensions
{
    internal static class StringExtensions
    {
        private static readonly string[] s_formatChevrons_oldValues = {"<", ">"};
        private static readonly string[] s_formatChevrons_newValues = {"&lt;", "&gt;"};
        public static string FormatChevrons(this string value)
        {
            return value.ReplaceMany(s_formatChevrons_oldValues, s_formatChevrons_newValues);
        }

        private static readonly string[] s_typeNameToFileName_oldValues = {"<", ">", ",", " ", "`"};
        private static readonly string[] s_typeNameToFileName_newValues = {"{", "}", "", "-", "-"};
        public static string MakeTypeNameFileNameSafe(string typeName)
        {
            return typeName.ReplaceMany(s_typeNameToFileName_oldValues, s_typeNameToFileName_newValues);
        }

        /// <summary>
        /// Returns whether the string is a valid RegEx pattern.
        /// </summary>
        /// <param name="pattern">The string representing the RegEx pattern.</param>
        /// <returns><see cref="true"/> if the string is a valid RegEx pattern; otherwise <see cref="false"/>.</returns>
        public static bool IsValidRegex(this string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private static readonly char[] s_globWildcards = {'*', '?', '['};
        private static readonly char[] s_pathSeparators = {'\\', '/'};
        /// <summary>
        /// Returns all files that fulfill the glob pattern.
        /// </summary>
        /// <param name="globFilePath">The glob file pattern.</param>
        /// <returns>A enumerable sequence of all files that fulfill the glob pattern.</returns>
        public static IEnumerable<string> GetGlobFiles(this string globFilePath)
        {
            if (!Path.IsPathRooted(globFilePath))
            {
                return Glob.Files(Environment.CurrentDirectory, globFilePath);
            }

            // Rooted path, begins with C: etc.
            if (globFilePath.IndexOfAny(s_globWildcards) == -1)
            {
                var fi = new FileInfo(globFilePath);
                return fi.Exists ? new[] {fi.FullName} : Enumerable.Empty<string>();
            }
            // Split the glob as close to wildcard as possible
            string[] pathPortions = globFilePath.Split(s_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            int portionBreakIndex = pathPortions.TakeWhile(portion => portion.IndexOfAny(s_globWildcards) == -1).Count();

            string directory = string.Join('\\', pathPortions, 0, portionBreakIndex);
            string glob = portionBreakIndex == pathPortions.Length ? string.Empty : string.Join('\\', pathPortions, portionBreakIndex, pathPortions.Length - portionBreakIndex);
            return Glob.Files(directory, glob).Select(relativePath => Path.Combine(directory, relativePath));
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
