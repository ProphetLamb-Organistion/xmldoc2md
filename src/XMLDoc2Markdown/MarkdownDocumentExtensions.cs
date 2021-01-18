using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Markdown;

namespace XMLDoc2Markdown
{
    internal static class MarkdownDocumentExtensions
    {
        public static IMarkdownDocument CreateBlockquoteDocument(this IMarkdownDocument document)
        {
            var qb = new BlockquoteDocument();
            document.Append(qb);
            return qb;
        }
    }

    internal class BlockquoteDocument : MarkdownDocument, IMarkdownBlockElement
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
}
