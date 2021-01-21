namespace XMLDoc2Markdown.Extensions
{
    internal static class VisibilityExtensions
    {
        internal static string Print(this Visibility visibility) =>
            visibility switch
            {
                Visibility.Public => "public",
                Visibility.Internal => "internal",
                Visibility.Protected => "protected",
                Visibility.ProtectedInternal => "protected internal",
                Visibility.Private => "private",
                _ => string.Empty
            };
    }
}
