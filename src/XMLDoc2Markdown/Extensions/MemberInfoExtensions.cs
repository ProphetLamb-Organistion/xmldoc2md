using System;
using System.Reflection;

namespace XMLDoc2Markdown.Extensions
{
    internal static class MemberInfoExtensions
    {
        internal static string GetSignature(this MemberInfo memberInfo, bool full = false) =>
            memberInfo switch
            {
                Type type => type.ToSymbol().GetSignature(full),
                MethodBase methodBase => methodBase.GetSignature(full),
                PropertyInfo propertyInfo => propertyInfo.GetSignature(full),
                EventInfo eventInfo => eventInfo.GetSignature(full),
                _ => throw new NotSupportedException()
            };
    }
}
