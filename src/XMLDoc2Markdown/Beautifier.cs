﻿using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace XMLDoc2Markdown
{
    public static class Beautifier
    {
        public static string BeautifyType(Type t, bool isFull = false)
        {
            if (t == null) return "";
            if (t == typeof(void)) return "void";
            if (!t.IsGenericType) return isFull ? t.FullName : t.Name;

            string innerFormat = string.Join(", ", t.GetGenericArguments().Select(x => BeautifyType(x)));
            return Regex.Replace(isFull ? t.GetGenericTypeDefinition().FullName : t.GetGenericTypeDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
        }

        public static string ToMarkdownMethodInfo(MethodInfo methodInfo)
        {
            bool isExtension = methodInfo.GetCustomAttributes<System.Runtime.CompilerServices.ExtensionAttribute>(false).Any();

            System.Collections.Generic.IEnumerable<string> seq = methodInfo.GetParameters().Select(x =>
            {
                string suffix = x.HasDefaultValue ? (" = " + (x.DefaultValue ?? $"null")) : "";
                return "`" + BeautifyType(x.ParameterType) + "` " + x.Name + suffix;
            });

            return methodInfo.Name + "(" + (isExtension ? "this " : "") + string.Join(", ", seq) + ")";
        }
    }
}
