using System;

namespace XMLDoc2Markdown.Extensions
{
    public static class ExceptionExtensions
    {
        public static string ToLog(this Exception ex)
        {
            return $"An exception of the type '{ex.GetType().Name}' occured in {ex.Source}: '{ex.Message}'\r\n"
              + $"Inner exception\t{ex.InnerException}\r\n\r\n{ex.StackTrace}";
        }

        public static string ToLog(this Exception ex, bool appendInnerException)
        {
            if (appendInnerException && ex.InnerException != null)
            {
                return ex.ToLog() + ex.InnerException!.ToLog();
            }

            return ex.ToLog();
        }
    }
}
