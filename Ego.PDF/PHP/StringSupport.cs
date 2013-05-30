namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods to work with strings.
    /// </summary>
    public class StringSupport
    {
        /// <summary>
        /// Quotes the specified string with backslashes. 
        /// The characters to be quoted are: quote('), double quote("), backslash(\) and null(0).
        /// </summary>
        /// <param name="theString">The string to be quoted with backslashes.</param>
        /// <returns>Returns the quoted string.</returns>
        public static string AddSlashes(string theString)
        {
            string tempStr = "";
            tempStr = theString.Replace("\\", "\\\\");
            tempStr = tempStr.Replace("\'", "\\\'");
            tempStr = tempStr.Replace("\"", "\\\"");
            tempStr = tempStr.Replace("\0", "\\0");
            return tempStr;
        }

        /// <summary>
        /// Un-Quotes the specified string with backslashes.
        /// The characters to be un-quoted are: quote('), double quote("), backslash(\) and null(0).
        /// </summary>
        /// <param name="theString">The string to be un-quoted with backslashes.</param>
        /// <returns>Returns the un-quoted string.</returns>
        public static string RemoveSlashes(string theString)
        {
            string tempStr = "";
            tempStr = theString.Replace("\\\\", "\\");
            tempStr = tempStr.Replace("\\\'", "\'");
            tempStr = tempStr.Replace("\\\"", "\"");
            tempStr = tempStr.Replace("\\0", "\0");
            return tempStr;
        }

        /// <summary>
        /// Converts a string containing binary data to a string with its hexadecimal representation.
        /// </summary>
        /// <param name="binString">The string with binary data to be converted.</param>
        /// <returns>Returns an ASCII string containing the hexadecimal representation of the binary string.</returns>
        public static string BinToHex(string binString)
        {
            string hexString = "";
            foreach (byte theByte in System.Text.Encoding.ASCII.GetBytes(binString))
                hexString += System.Convert.ToString(System.Convert.ToInt32(theByte), 16).PadLeft(2, '0');
            return hexString;
        }

        /// <summary>
        /// Shuffles a string value using the support random seeded instance.
        /// </summary>
        /// <param name="input">The string to shuffle.</param>
        /// <returns>Returns a new shuffled string.</returns>
        public static string StringShuffle(string input)
        {
            System.Text.StringBuilder oldString = new System.Text.StringBuilder(input);
            System.Text.StringBuilder newString = new System.Text.StringBuilder();
            newString.Length = input.Length;

            for (int charIndex = oldString.Length - 1; charIndex >= 0; charIndex--)
            {
                //Use the support random seeded instance.
                int newIndex = MathSupport.Rand.Next(0, charIndex);
                newString[charIndex] = oldString[newIndex];
                oldString[newIndex] = oldString[charIndex];
            }
            return newString.ToString();
        }

        /// <summary>
        /// Joins the elements specified in the OrderedMap parameter using the supplied separator.
        /// </summary>
        /// <param name="separator">The string that will be used as separator for the concatenated elements.</param>
        /// <param name="array">The OrderedMap which elements will be used to concatenate.</param>
        /// <returns>Returns a string consisting of the elements of the OrderedMap parameter joined by the separator parameter.</returns>
        public static string Join(string separator, OrderedMap array)
        {
            string[] elements = new string[array.Count];

            for (int i = 0; i < array.Count; i++)
                elements[i] = array.GetValueAt(i).ToString();

            return string.Join(separator, elements);
        }

        /// <summary>
        /// Returns localized numeric and monetary formatting information.
        /// </summary>
        /// <returns>Returns an OrderedMap containing localized numeric and monetary formatting information.</returns>
        public static OrderedMap GetNumberFormatInfo()
        {
            OrderedMap array = new OrderedMap();

            array.Add("decimal_point", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
            array.Add("thousands_sep", System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator);
            //array.Add("grouping", );
            //array.Add("int_curr_symbol", );
            array.Add("currency_symbol", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencySymbol);
            array.Add("mon_decimal_point", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator);
            array.Add("mon_thousands_sep", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator);
            //array.Add("mon_grouping", );
            array.Add("positive_sign", System.Globalization.NumberFormatInfo.CurrentInfo.PositiveSign);
            array.Add("negative_sign", System.Globalization.NumberFormatInfo.CurrentInfo.NegativeSign);
            array.Add("int_frac_digits", System.Globalization.NumberFormatInfo.InvariantInfo.NumberDecimalDigits);
            array.Add("frac_digits", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalDigits);
            //array.Add("p_cs_precedes", );
            //array.Add("p_sep_by_space", );
            //array.Add("n_cs_precedes", );
            //array.Add("n_sep_by_space", );
            //array.Add("p_sign_posn", );
            //array.Add("n_sign_posn", );

            return array;
        }

        /// <summary>
        /// Calculates the md5 hash of a given file.
        /// </summary>
        /// <param name="filename">The name of the file which contents will be used to calculate the md5 hash.</param>
        /// <returns>Returns a string value representing the calculated md5 hash.</returns>
        public static string ComputeMD5FromFile(string filename)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider crypto =
                new System.Security.Cryptography.MD5CryptoServiceProvider();
            System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
            System.IO.FileStream stream;
            string result;

            try
            {
                stream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                result = utf.GetString(crypto.ComputeHash(stream));
            }
            catch (System.Exception exception)
            {
                throw exception;
            }

            return result;
        }

        /// <summary>
        /// Calculates the md5 hash of a given string.
        /// </summary>
        /// <param name="str">The string that will be used to calculate the md5 hash.</param>
        /// <returns>Returns a string value representing the calculated md5 hash.</returns>
        public static string ComputeMD5(string str)
        {
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "md5").ToLower();
        }

        /// <summary>
        /// Inserts HTML line breaks before all newlines in a string.
        /// </summary>
        /// <param name="str">The string where the newline sequence will be replaced.</param>
        /// <returns>Returns string with newline sequences replaced with '<br />'.</returns>
        public static string Nl2br(string str)
        {
            string result = str;

            if (str.IndexOf("\r\n") > -1)
                result = str.Replace("\r\n", "<br />");
            else if (str.IndexOf("\n\r") > -1)
                result = str.Replace("\n\r", "<br />");
            else if (str.IndexOf("\r") > -1)
                result = str.Replace("\r", "<br />");
            else if (str.IndexOf("\n") > -1)
                result = str.Replace("\n", "<br />");

            return result;
        }

        /// <summary>
        /// Adds a backslash character (\) before every character that is among the following: . \\ + * ? [ ^ ] ( $ )
        /// </summary>
        /// <param name="str">The string where the characters will be escaped.</param>
        /// <returns>Returns a string with a backslash character before every special character.</returns>
        public static string QuoteMeta(string str)
        {
            string tempStr = "";
            tempStr = str.Replace(@"\", @"\\");
            tempStr = tempStr.Replace(".", "\\.");
            tempStr = tempStr.Replace("+", "\\+");
            tempStr = tempStr.Replace("*", "\\*");
            tempStr = tempStr.Replace("?", "\\?");
            tempStr = tempStr.Replace("[", "\\[");
            tempStr = tempStr.Replace("^", "\\^");
            tempStr = tempStr.Replace("]", "\\]");
            tempStr = tempStr.Replace("(", "\\(");
            tempStr = tempStr.Replace("$", "\\$");
            tempStr = tempStr.Replace(")", "\\)");
            return tempStr;
        }

        /// <summary>
        /// Calculates the md5 hash of a given file.
        /// </summary>
        /// <param name="filename">The name of the file which contents will be used to calculate the md5 hash.</param>
        /// <returns>Returns a string value representing the calculated md5 hash.</returns>
        public static string ComputeSHA1FromFile(string filename)
        {
            System.Security.Cryptography.SHA1Managed crypto = new System.Security.Cryptography.SHA1Managed();
            System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
            System.IO.FileStream stream;
            string result;

            try
            {
                stream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                result = utf.GetString(crypto.ComputeHash(stream));
            }
            catch (System.Exception exception)
            {
                throw exception;
            }

            return result;
        }

        /// <summary>
        /// Calculates the md5 hash of a given string.
        /// </summary>
        /// <param name="str">The string that will be used to calculate the md5 hash.</param>
        /// <returns>Returns a string value representing the calculated md5 hash.</returns>
        public static string ComputeSHA1(string str)
        {
            System.Security.Cryptography.SHA1Managed crypto = new System.Security.Cryptography.SHA1Managed();
            System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
            string result;

            try
            {
                result = utf.GetString(crypto.ComputeHash(utf.GetBytes(str)));
            }
            catch (System.Exception exception)
            {
                throw exception;
            }

            return result;
        }

        /// <summary>
        /// Pads a string to a certain length with the first character of the provided string.
        /// </summary>
        /// <param name="input">The string to pad.</param>
        /// <param name="length">The padding length.</param>
        /// <param name="padString">The string whose first character will be used to pad 'input'.</param>
        /// <param name="type">The type of padding.
        /// <list type="bullet">
        ///   <item><description>0: Pad right.</description></item>
        ///   <item><description>1: Pad left.</description></item>
        ///   <item><description>2: Pad both.</description></item>
        /// </list>
        /// </param>
        /// <returns>Returns a new string that is equivalent 'input' but padded on the left, right or both sides 
        /// using first character of 'padString' and with a total length specified by 'lenght'.</returns>
        public static string Pad(string input, int length, string padString, int type)
        {
            string result = "";

            switch (type)
            {
                    //PAD_RIGHT
                case 0:
                    result = input.PadRight(length, padString[0]);
                    break;
                    //PAD_LEFT
                case 1:
                    result = input.PadLeft(length, padString[0]);
                    break;
                    //PAD_BOTH
                case 2:
                    result =
                        input.PadLeft(
                            input.Length + (int) System.Math.Floor(((double) length - (double) input.Length)/2),
                            padString[0]);
                    result = result.PadRight(length, padString[0]);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Makes a binary comparison between the two specified strings. The case sensitiveness is parameter dependant.
        /// </summary>
        /// <param name="input1">The first string to compare.</param>
        /// <param name="input2">The second string to compare.</param>
        /// <param name="caseSensitive">The boolean value that specifies the case sensitiveness, if true, the comparison will be case-sensitive.</param>
        /// <returns>Returns:
        /// <list type="bullet">
        /// <item><description>-1: If the first string is less than the second one.</description></item>
        /// <item><description>1: If the second string is less than the first one.</description></item>
        /// <item><description>0: If both strings are equals.</description></item>
        /// </list>
        /// </returns>
        public static int StringCompare(string input1, string input2, bool caseSensitive)
        {
            int result = 0;
            string theInput1 = input1;
            string theInput2 = input2;

            if (!caseSensitive)
            {
                theInput1 = theInput1.ToLower();
                theInput2 = theInput2.ToLower();
            }

            if (theInput1 != theInput2)
            {
                if (theInput1.Length < theInput2.Length)
                    result = -1;
                else if (theInput1.Length > theInput2.Length)
                    result = 1;
                else
                {
                    long res1 = SumStringBytes(theInput1);
                    long res2 = SumStringBytes(theInput2);
                    if (res1 < res2)
                        result = -1;
                    else
                        result = 1;
                }
            }
            return result;
        }

        /// <summary>
        /// Makes a binary comparison between the two specified strings. The case sensitiveness is parameter dependant.
        /// </summary>
        /// <param name="input1">The first string to compare.</param>
        /// <param name="input2">The second string to compare.</param>
        /// <param name="caseSensitive">The boolean value that specifies the case sensitiveness, if true, the comparison will be case-sensitive.</param>
        /// <param name="length">The number of characters to be compared on each string.</param>
        /// <returns>Returns:
        /// <list type="bullet">
        /// <item><description>-1: If the first string is less than the second one.</description></item>
        /// <item><description>1: If the second string is less than the first one.</description></item>
        /// <item><description>0: If both strings are equals.</description></item>
        /// </list></returns>
        public static int StringCompare(string input1, string input2, bool caseSensitive, int length)
        {
            return StringCompare(input1.Length <= length ? input1 : input1.Substring(0, length),
                                 input2.Length <= length ? input2 : input2.Substring(0, length), caseSensitive);
        }

        /// <summary>
        /// Sums the byte value of each character of the specified string.
        /// </summary>
        /// <param name="input">The string which contains the characters to sum.</param>
        /// <returns>Returns the sum of the byte value of each character of the specified string.</returns>
        private static long SumStringBytes(string input)
        {
            long sum = 0;
            for (int index = 0; index < input.Length; index++)
            {
                sum += (byte) input[index];
            }
            return sum;
        }

        /// <summary>
        /// Returns the substring that starts at the last index of <code>occurrence</code> of the specified string.
        /// </summary>
        /// <param name="input">The string to obtaing the substring from.</param>
        /// <param name="occurrence">The object used to obtain the last index.</param>
        /// <returns>Returns the resulting substring.</returns>
        public static string LastSubstring(string input, object occurrence)
        {
            string result = string.Empty, startChar = "";

            if (occurrence is string)
                startChar = ((string) occurrence).Substring(0, 1);
            else if (occurrence is int)
                startChar = new string(System.Convert.ToChar((int) occurrence), 1);
            else
                startChar = occurrence.ToString();

            int index = input.LastIndexOf(startChar);
            if (index != -1)
                result = input.Substring(index);

            return result;
        }

        /// <summary>
        /// Reverses the specified string.
        /// </summary>
        /// <param name="input">The string to be reversed.</param>
        /// <returns>Returns the reversed string.</returns>
        public static string StringReverse(string input)
        {
            char[] theString = input.ToCharArray();
            System.Array.Reverse(theString);
            return new string(theString);
        }

        /// <summary>
        /// Returns the substring that starts at the last index of <code>occurrence</code> of the specified string.
        /// </summary>
        /// <param name="input">The string to obtaing the substring from.</param>
        /// <param name="occurrence">The object used to obtain the last index.</param>
        /// <returns>Returns the resulting substring.</returns>
        public static int LastIndexOf(string input, object occurrence)
        {
            string startChar = "";

            if (occurrence is string)
                startChar = ((string) occurrence).Substring(0, 1);
            else if (occurrence is int)
                startChar = new string(System.Convert.ToChar((int) occurrence), 1);
            else
                startChar = occurrence.ToString();

            return input.LastIndexOf(startChar);
        }

        private static string tokenizedString;

        /// <summary>
        /// Tokenizes the specified string using the specified separators.
        /// </summary>
        /// <param name="input">The string to tokenized.</param>
        /// <param name="separators">The string that contains the separators.</param>
        /// <returns>Returns the current token.</returns>
        public static string StringTokenizer(string input, string separators)
        {
            string tokenList = input, currentToken = "";
            char[] separatorArray = separators.ToCharArray();
            string[] tokensArray;

            tokenizedString = input;

            while (currentToken == "")
            {
                tokensArray = tokenizedString.Split(separatorArray, 2);
                if (tokensArray.Length == 2)
                {
                    currentToken = tokensArray[0];
                    tokenizedString = tokensArray[1];
                }
                else
                {
                    currentToken = tokensArray[0];
                    tokenizedString = "";
                    break;
                }
            }

            return currentToken;
        }

        /// <summary>
        /// Tokenizes a previously set string (to set the string to be tokenized, use the other overloaded method) using the specified separators.
        /// </summary>
        /// <param name="separators">The string that contains the separators.</param>
        /// <returns>Returns the current token.</returns>
        public static string StringTokenizer(string separators)
        {
            return StringTokenizer(tokenizedString, separators);
        }


        /// <summary>
        /// Replaces each character of <code>toFind</code> with the corresponding ones in <code>replaceWith</code> of the specified string.
        /// </summary>
        /// <param name="input">The string to used to do the replacements.</param>
        /// <param name="toFind">The string that contains the characters to find.</param>
        /// <param name="replaceWith">The string that contains the characters to replace with.</param>
        /// <returns>Returns a new replaced string.</returns>
        public static string StringReplace(string input, string toFind, string replaceWith)
        {
            int maxToReplace = toFind.Length <= replaceWith.Length ? toFind.Length : replaceWith.Length;
            string target = input;
            for (int index = 0; index < maxToReplace; index++)
            {
                target = target.Replace(toFind[index], replaceWith[index]);
            }
            return target;
        }

        /// <summary>
        /// Counts the number of ocurrences of the specified substring in the specified string.
        /// </summary>
        /// <param name="input">The string where the substring is searched.</param>
        /// <param name="toFind">The substring whose occurrences will be counted.</param>
        /// <returns>Returns the number of occurrences of the specified substring.</returns>
        public static int SubstringCount(string input, string subString)
        {
            string theInput = input;
            int result = 0, index = 0;

            while (index != -1)
            {
                index = theInput.IndexOf(subString);
                if (index != -1)
                {
                    result++;
                    theInput = theInput.Remove(index, subString.Length);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a string with the first letter in uppercase.
        /// </summary>
        /// <param name="input">The string which whose first character will be changed.</param>
        /// <returns>Returns a string with the first letter in uppercase.</returns>
        public static string StringToUpperFirst(string input)
        {
            string result = "";
            if (input.Length > 1)
            {
                string toUpper = input.Substring(0, 1);
                toUpper = toUpper.ToUpper();
                result = toUpper + input.Substring(1);
            }
            else if (input.Length == 1)
                result = input.ToUpper();

            return result;
        }
    }
}