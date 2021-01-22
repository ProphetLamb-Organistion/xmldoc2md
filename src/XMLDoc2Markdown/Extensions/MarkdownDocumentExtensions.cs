using System;
using System.Text;

using Markdown;

namespace XMLDoc2Markdown.Extensions
{
    internal static class MarkdownDocumentExtensions
    {
        public static IMarkdownDocument CreateBlockquoteDocument(this IMarkdownDocument document)
        {
            var qb = new BlockquoteDocument();
            document.Append(qb);
            return qb;
        }

        public static IMarkdownBlockElement BlockFromString(string content)
        {
            return new RawMarkdownElement(content);
        }

        private sealed class BlockquoteDocument : MarkdownDocument, IMarkdownBlockElement
        {
            private static readonly string s_newLineQuoteBlock = Environment.NewLine + "> ";

            public override string ToString()
            {
                var sb = new StringBuilder(base.ToString());
                sb.Replace(Environment.NewLine, s_newLineQuoteBlock);
                sb.Insert(0, "> ");
                sb.Append(Environment.NewLine);
                return sb.ToString();
            }
        }

        private sealed class RawMarkdownElement : IMarkdownBlockElement
        {
            private readonly string _content;

            public RawMarkdownElement(string content)
            {
                this._content = content;
            }

            public override string ToString() => this._content;
        }
    }
}
