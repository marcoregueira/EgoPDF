using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ego.PDF.Support
{
    public static class StringSupport
    {
        public static int SubstringCount(this string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }
    }
}
