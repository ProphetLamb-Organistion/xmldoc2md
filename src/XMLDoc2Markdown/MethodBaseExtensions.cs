using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XMLDoc2Markdown
{
    internal static class MethodBaseExtensions
    {
        internal static Visibility GetVisibility(this MethodBase methodBase)
        {
            if (methodBase.IsPublic)
            {
                return Visibility.Public;
            }
            else if (methodBase.IsAssembly)
            {
                return Visibility.Internal;
            }
            else if (methodBase.IsFamily)
            {
                return Visibility.Protected;
            }
            else if (methodBase.IsFamilyOrAssembly)
            {
                return Visibility.ProtectedInternal;
            }
            else if (methodBase.IsPrivate)
            {
                return Visibility.Private;
            }
            else
            {
                return Visibility.None;
            }
        }

        internal static string GetSignature(this MethodBase methodBase, bool full = false)
        {
            var signature = new List<string>();

            if (full)
            {
                if (methodBase.DeclaringType.IsClass)
                {
                    signature.Add(methodBase.GetVisibility().Print());

                    if (methodBase.IsStatic)
                    {
                        signature.Add("static");
                    }

                    if (methodBase.IsAbstract)
                    {
                        signature.Add("abstract");
                    }
                }

                if (methodBase is MethodInfo methodInfo)
                {
                    signature.Add(methodInfo.ReturnType.GetSimplifiedName());
                }
            }

            string displayName = methodBase.MemberType == MemberTypes.Constructor ? methodBase.DeclaringType?.Name : methodBase.Name;
            if (displayName is null)
            {
                throw new InvalidOperationException("Constructor members must have DeclaringType defined. Name cannot be null");
            }
            int genericCharIndex = displayName.IndexOf('`');
            if (genericCharIndex > -1)
            {
                displayName = displayName.Substring(0, genericCharIndex);
            }
            if (methodBase is MethodInfo methodInfo1)
            {
                Type[] genericArguments = methodInfo1.GetGenericArguments();
                if  (genericArguments.Length > 0)
                {
                    displayName += $"<{string.Join(", ", genericArguments.Select(a => a.GetDisplayName()))}>";
                }
            }
            ParameterInfo[] @params = methodBase.GetParameters();
            IEnumerable<string> paramsNames = @params
                .Select(p => $"{(full ? p.ParameterType.GetSimplifiedName() : p.ParameterType.GetDisplayName())}{(full ? $" {p.Name}" : null)}");
            displayName += $"({string.Join(", ", paramsNames)})";
            signature.Add(displayName);

            return string.Join(' ', signature);
        }
    }
}
