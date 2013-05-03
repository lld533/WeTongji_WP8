using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace WeTongji.Utility
{
    public static class DescriptionToInlineCollection
    {
        public static IEnumerable<TextBlock> GetInlineCollection(this String str)
        {
            var result = new List<TextBlock>();

            string[] substrings = Regex.Split(str, "\\r\\n");

            TextBlock tb = new TextBlock();
            Boolean b = false;

            foreach (var s in substrings)
            {
                if (tb.Inlines.Count == 10)
                {
                    result.Add(tb);
                    tb = new TextBlock();
                    b = true;
                }
                else
                    b = false;

                if (String.IsNullOrEmpty(s))
                {
                    tb.Inlines.Add(new LineBreak());
                }
                else
                {
                    tb.Inlines.Add(new Run() { Text = s });

                    if (tb.Inlines.Count == 10)
                    {
                        result.Add(tb);
                        tb = new TextBlock();
                        b = true;
                    }
                    else
                        b = false;

                    tb.Inlines.Add(new LineBreak());
                }
            }

            if (!b)
                result.Add(tb);

            return result;
        }
    }
}
