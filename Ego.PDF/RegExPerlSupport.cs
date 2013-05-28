namespace PHP
{
    /// <summary>
    /// Provides static methods related to Perl Regular Expressions functions.
    /// </summary>
    public class RegExPerlSupport
    {
        /// <summary>
        /// Performs a regular expression match. The OrderedMap parameter is populated with the matched 
        /// substrings. If flags is provided and is equal to 256 the index of the matched substring will
        /// also be included in the OrderedMap parameter.
        /// </summary>
        /// <param name="pattern">The pattern to search in subject.</param>
        /// <param name="subject">The subject string where the pattern will be applied.</param>
        /// <param name="matches">An OrderedMap object where the matched string will be stored, and its index
        /// in subject if the flags param equals 256.</param>
        /// <param name="flags">If set with 256, the index of the matched substring will be added to the
        /// OrderedMao parameter.</param>
        /// <returns>Returns 0 if no matches were found and 1 if a match was found.</returns>
        public static int Match(string pattern, string subject, ref OrderedMap matches, int flags)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var match = regex.Match(subject);

            if (match.Success == false)
            {
                matches = new OrderedMap();
                return 0;
            }
            matches = flags == 256
                          ? new OrderedMap(new OrderedMap(new object[] {match.Groups[0].Value, match.Groups[0].Index}))
                          : new OrderedMap(match.Groups[0].Value);
            return 1;
        }

        /// <summary>
        /// Perform a global regular expression match on the specified subject string.
        /// </summary>
        /// <param name="pattern">The pattern to search in subject.</param>
        /// <param name="subject">The subject string where the pattern will be applied.</param>
        /// <param name="matches">An OrderedMap object where the matched strings will be stored.</param>
        /// <returns>Returns the number of matches found in the subject string.</returns>
        public static int MatchAll(string pattern, string subject, ref OrderedMap matches)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern,
                                                                 System.Text
                                                                       .RegularExpressions
                                                                       .RegexOptions
                                                                       .Compiled);
            System.Text.RegularExpressions.Match match;
            matches = new OrderedMap();
            var count = 0;
            for (match = regex.Match(subject); match.Success; match = match.NextMatch())
            {
                var innerMatches = new OrderedMap();
                for (var i = 0; i < match.Groups.Count; i++)
                {
                    innerMatches.Add(i, match.Groups[i].Value);
                }
                matches.Add(count, innerMatches);
                count++;
            }

            return match.Groups.Count;
        }

        /// <summary>
        /// Splits the subject string at the position defined by the given regular expression.
        /// The limit parameter specifies the maximum number of times the string is to be split.
        /// </summary>
        /// <param name="pattern">The pattern to search in subject.</param>
        /// <param name="subject">The subject string where the pattern will be applied.</param>
        /// <param name="limit">The maximum number of array elements to return.</param>
        /// <returns>Returns an OrderedMap object containing substrings of subject split along 
        /// boundaries matched by pattern. </returns>
        public static OrderedMap Split(string pattern, string subject, int limit)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var result = limit < 0 ? regex.Split(subject) : regex.Split(subject, limit);
            var array = new OrderedMap();
            for (var i = 0; i < result.Length; i++)
                array.Add(i, result[i]);

            return array;
        }

        /// <summary>
        /// Return the elements of the input OrderedMap that match the given pattern.
        /// </summary>
        /// <param name="pattern">The pattern to search in each OrderedMap entry.</param>
        /// <param name="input">The OrderedMap object whose elements will be searched for pattern.</param>
        /// <returns>An OrderedMap populated with the elements of input that match pattern.</returns>
        public static OrderedMap MatchArray(string pattern, OrderedMap input)
        {
            var result = new OrderedMap();
            var regex = new System.Text.RegularExpressions.Regex(pattern);

            foreach (object obj in input.Values)
            {
                if (regex.Match(obj.ToString()).Success)
                    result.Push(new object[] {obj});
            }

            return result;
        }
    }
}