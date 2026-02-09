using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using RCommon.Util;
using System.Linq.Expressions;
using System.Globalization;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/> operations including validation, formatting,
    /// encoding, hashing, case conversion, and various string manipulation utilities.
    /// </summary>
    public static class StringExtension
    {
        private static readonly Regex WebUrlExpression = new Regex(@"(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex EmailExpression = new Regex(@"^([0-9a-zA-Z]+[-._+&])*[0-9a-zA-Z]+@([-0-9a-zA-Z]+[.])+[a-zA-Z]{2,6}$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StripHTMLExpression = new Regex("<\\S[^><]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly char[] IllegalUrlCharacters = new[] { ';', '/', '\\', '?', ':', '@', '&', '=', '+', '$', ',', '<', '>', '#', '%', '.', '!', '*', '\'', '"', '(', ')', '[', ']', '{', '}', '|', '^', '`', '~', '–', '‘', '’', '“', '”', '»', '«' };

        /// <summary>
        /// Determines whether the string is a valid HTTP or HTTPS web URL.
        /// </summary>
        /// <param name="target">The string to test.</param>
        /// <returns><c>true</c> if the string is a valid web URL; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool IsWebUrl(this string target)
        {
            return !string.IsNullOrEmpty(target) && WebUrlExpression.IsMatch(target);
        }

        /// <summary>
        /// Determines whether the string is a valid email address.
        /// </summary>
        /// <param name="target">The string to test.</param>
        /// <returns><c>true</c> if the string is a valid email address; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool IsEmail(this string target)
        {
            return !string.IsNullOrEmpty(target) && EmailExpression.IsMatch(target);
        }

        /// <summary>
        /// Returns the string trimmed, or <see cref="string.Empty"/> if the string is null.
        /// </summary>
        /// <param name="target">The string to make null-safe.</param>
        /// <returns>The trimmed string, or empty string if null.</returns>
        [DebuggerStepThrough]
        public static string NullSafe(this string? target)
        {
            return (target ?? string.Empty).Trim();
        }

        /// <summary>
        /// Formats the string using the current culture with the specified arguments.
        /// </summary>
        /// <param name="target">The format string template.</param>
        /// <param name="args">The arguments to substitute into the format string.</param>
        /// <returns>The formatted string.</returns>
        [DebuggerStepThrough]
        public static string FormatWith(this string target, params object[] args)
        {
            Guard.IsNotEmpty(target, "target");

            return string.Format(Constants.CurrentCulture, target, args);
        }

        /// <summary>
        /// Computes an MD5 hash of the string and returns it as a Base64-encoded string.
        /// </summary>
        /// <param name="target">The string to hash.</param>
        /// <returns>A Base64-encoded MD5 hash of the string.</returns>
        [DebuggerStepThrough]
        public static string Hash(this string target)
        {
            Guard.IsNotEmpty(target, "target");

            using (MD5 md5 = MD5.Create())
            {
                byte[] data = Encoding.Unicode.GetBytes(target);
                byte[] hash = md5.ComputeHash(data);

                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Truncates the string at the specified index and appends an ellipsis ("...") if the string exceeds that length.
        /// </summary>
        /// <param name="target">The string to truncate.</param>
        /// <param name="index">The maximum length including the ellipsis.</param>
        /// <returns>The original string if shorter than <paramref name="index"/>, or the truncated string with ellipsis.</returns>
        [DebuggerStepThrough]
        public static string WrapAt(this string target, int index)
        {
            const int DotCount = 3;

            Guard.IsNotEmpty(target, "target");
            Guard.IsNotNegativeOrZero(index, "index");

            return (target.Length <= index) ? target : string.Concat(target.Substring(0, index - DotCount), new string('.', DotCount));
        }

        /// <summary>
        /// Removes all HTML tags from the string.
        /// </summary>
        /// <param name="target">The string to strip HTML from.</param>
        /// <returns>The string with all HTML tags removed.</returns>
        [DebuggerStepThrough]
        public static string StripHtml(this string target)
        {
            return StripHTMLExpression.Replace(target, string.Empty);
        }

        /// <summary>
        /// Converts a 22-character URL-safe Base64-encoded string back to a <see cref="Guid"/>.
        /// </summary>
        /// <param name="target">The 22-character Base64-encoded GUID string (with <c>-</c> and <c>_</c> as safe characters).</param>
        /// <returns>The decoded <see cref="Guid"/>, or <see cref="Guid.Empty"/> if the input is invalid.</returns>
        [DebuggerStepThrough]
        public static Guid ToGuid(this string target)
        {
            Guid result = Guid.Empty;

            if ((!string.IsNullOrEmpty(target)) && (target.Trim().Length == 22))
            {
                string encoded = string.Concat(target.Trim().Replace("-", "+").Replace("_", "/"), "==");

                try
                {
                    byte[] base64 = Convert.FromBase64String(encoded);

                    result = new Guid(base64);
                }
                catch (FormatException)
                {
                }
            }

            return result;
        }

        /// <summary>
        /// Converts the string to an enum value, returning a default value if conversion fails.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="target">The string to convert.</param>
        /// <param name="defaultValue">The default value to return if parsing fails.</param>
        /// <returns>The parsed enum value, or <paramref name="defaultValue"/> if parsing fails.</returns>
        [DebuggerStepThrough]
        public static T ToEnum<T>(this string target, T defaultValue) where T : IComparable, IFormattable
        {
            T convertedValue = defaultValue;

            if (!string.IsNullOrEmpty(target))
            {
                try
                {
                    convertedValue = (T)Enum.Parse(typeof(T), target.Trim(), true);
                }
                catch (ArgumentException)
                {
                }
            }

            return convertedValue;
        }

        /// <summary>
        /// Removes illegal URL characters from the string and replaces spaces with hyphens to produce a URL-safe string.
        /// </summary>
        /// <param name="target">The string to convert.</param>
        /// <returns>A URL-safe string, or the original string if it is null or empty.</returns>
        [DebuggerStepThrough]
        public static string ToLegalUrl(this string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return target;
            }

            target = target.Trim();

            if (target.IndexOfAny(IllegalUrlCharacters) > -1)
            {
                foreach (char character in IllegalUrlCharacters)
                {
                    target = target.Replace(character.ToString(Constants.CurrentCulture), string.Empty);
                }
            }

            target = target.Replace(" ", "-");

            while (target.Contains("--"))
            {
                target = target.Replace("--", "-");
            }

            return target;
        }

        /// <summary>
        /// URL-encodes the string using <see cref="HttpUtility.UrlEncode(string)"/>.
        /// </summary>
        /// <param name="target">The string to encode.</param>
        /// <returns>The URL-encoded string.</returns>
        [DebuggerStepThrough]
        public static string? UrlEncode(this string target)
        {
            return HttpUtility.UrlEncode(target);
        }

        /// <summary>
        /// URL-decodes the string using <see cref="HttpUtility.UrlDecode(string)"/>.
        /// </summary>
        /// <param name="target">The string to decode.</param>
        /// <returns>The URL-decoded string.</returns>
        [DebuggerStepThrough]
        public static string? UrlDecode(this string target)
        {
            return HttpUtility.UrlDecode(target);
        }

        /// <summary>
        /// HTML-attribute-encodes the string using <see cref="HttpUtility.HtmlAttributeEncode(string)"/>.
        /// </summary>
        /// <param name="target">The string to encode.</param>
        /// <returns>The HTML-attribute-encoded string.</returns>
        [DebuggerStepThrough]
        public static string AttributeEncode(this string target)
        {
            return HttpUtility.HtmlAttributeEncode(target);
        }

        /// <summary>
        /// HTML-encodes the string using <see cref="HttpUtility.HtmlEncode(string)"/>.
        /// </summary>
        /// <param name="target">The string to encode.</param>
        /// <returns>The HTML-encoded string.</returns>
        [DebuggerStepThrough]
        public static string HtmlEncode(this string target)
        {
            return HttpUtility.HtmlEncode(target);
        }

        /// <summary>
        /// HTML-decodes the string using <see cref="HttpUtility.HtmlDecode(string)"/>.
        /// </summary>
        /// <param name="target">The string to decode.</param>
        /// <returns>The HTML-decoded string.</returns>
        [DebuggerStepThrough]
        public static string HtmlDecode(this string target)
        {
            return HttpUtility.HtmlDecode(target);
        }

        /// <summary>
        /// Replaces all occurrences of each string in <paramref name="oldValues"/> with <paramref name="newValue"/>.
        /// </summary>
        /// <param name="target">The string to perform replacements on.</param>
        /// <param name="oldValues">The collection of strings to replace.</param>
        /// <param name="newValue">The replacement string.</param>
        /// <returns>The modified string with all replacements applied.</returns>
        public static string Replace(this string target, ICollection<string> oldValues, string newValue)
        {
            oldValues.ForEach(oldValue => target = target.Replace(oldValue, newValue));
            return target;
        }

        /// <summary>
        /// Trims the leading and trailing spaces from the input string, and converts an empty string to null.
        /// </summary>
        /// <param name="inputString">the input string to be converted</param>
        /// <returns>The converted string</returns>
        public static string? ToNullIfEmptyOrBlank(this string? inputString)
        {
            if (inputString == null || (inputString = inputString.Trim()).Length == 0) return null;

            return inputString;
        }

        /// <summary>
        ///  Tests if string is null, empty, or all blanks
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyOrBlank(this string? inputString)
        {
            return (inputString == null || inputString.Trim().Length == 0);
        }


        /// <summary>
        ///  Tests if the inputString string is numeric
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static bool IsNumeric(this string inputString)
        {
            Guard.IsNotNull(inputString, "inputString");

            foreach (char c in inputString)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a string to a byte array using ASCII encoding
        /// </summary>
        /// <param name="stringToConvert">The string to be converted</param>
        /// <returns>Byte Array containing data from input string</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="input"/> is null</exception>
        public static byte[] ToByteArray(this string stringToConvert)
        {
            Guard.IsNotNull(stringToConvert, "stringToConvert");

            byte[] bytes = Encoding.ASCII.GetBytes(stringToConvert);
            return bytes;
        }


        /// <summary>
        /// Remove non-digit chars from inputString string
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static string ReturnNumericCharsOnly(this string inputString)
        {
            Guard.IsNotNull(inputString, "inputString");

            // use StringBuilder for efficient string concat
            StringBuilder sb = new StringBuilder(string.Empty);

            foreach (char c in inputString)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
            }

            // return only numbers
            return sb.ToString();
        }


        /// <summary>
        /// Remove non-digit chars (including the comma separate) but keep 
        /// decimal point from inputString string
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static string ReturnDecimalCharsExcludingCommaSeparaters(this string inputString)
        {
            Guard.IsNotNull(inputString, "inputString");

            // use StringBuilder for efficient string concat
            StringBuilder sb = new StringBuilder(string.Empty);

            foreach (char c in inputString)
            {
                if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    sb.Append(c);
                }
            }

            // Return only numbers, the negative sign and decimal point.
            return sb.ToString();
        }

        /// <summary>
        /// Remove non-alpha chars from inputString string
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static string ReturnAlphaCharsOnly(this string inputString)
        {
            Guard.IsNotNull(inputString, "inputString");

            // use StringBuilder for efficient string concat
            StringBuilder sb = new StringBuilder();

            foreach (char c in inputString)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(c);
                }
            }

            // return only alpha chars
            return sb.ToString();
        }

        /// <summary>
        /// Remove non-alpha chars from inputString string
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static string ReturnAlphanumericCharsOnly(this string inputString)
        {
            Guard.IsNotNull(inputString, "inputString");

            // use StringBuilder for efficient string concat
            StringBuilder sb = new StringBuilder();

            foreach (char c in inputString)
            {
                if (char.IsLetter(c) || char.IsDigit(c))
                {
                    sb.Append(c);
                }
            }

            // return only alphanumeric chars
            return sb.ToString();
        }

        /// <summary>
        /// Pad with leading zero if needed.
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="inputString"/> is null</exception>
        public static string PadWithLeadingZeros(this string inputString, int maxLen)
        {
            Guard.IsNotNull(inputString, "inputString");
            return inputString.PadLeft(maxLen, '0');
        }

		/// <summary>
		/// Allows for culture and case sensitive and insensitive string searching.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="value"></param>
		/// <param name="comparer"></param>
		/// <returns></returns>
		public static bool Contains(this string target, string value, StringComparison comparer)
		{
			return target.IndexOf(value, 0, comparer) >= 0;
		}

		/// <summary>
		/// Allows for culture and case sensitive and insensitive string searching.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="value"></param>
		/// <param name="ignoreCase"></param>
		/// <returns></returns>
		public static bool Contains(this string target, string value, bool ignoreCase)
		{
			if (ignoreCase)
			{
				return Contains(target, value, StringComparison.InvariantCultureIgnoreCase);
			}
			else
			{
				return target.Contains(value); // It would be silly to use this method in this case, but we account for silliness.
			}
		}

        /// <summary>
        /// Substring with elipses but OK if shorter, will take 3 characters off character count if necessary
        /// </summary>
        public static string LimitWithElipses(this string str, int characterCount)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (characterCount < 5) return str.Limit(characterCount);       // Can’t do much with such a short limit
                if (str.Length <= characterCount - 3) return str;
                else return str.Substring(0, characterCount - 3) + "...";

            }
            return string.Empty;
        }

        /// <summary>
        /// Substring but OK if shorter
        /// </summary>
        public static string Limit(this string str, int characterCount)
        {
            if (str.Length <= characterCount) return str;
            else return str.Substring(0, characterCount).TrimEnd(' ');
        }

        /// <summary>
        /// Compares two strings for equality, ignoring case by default.
        /// </summary>
        /// <param name="target">The first string.</param>
        /// <param name="stringToCompare">The string to compare against.</param>
        /// <returns><c>true</c> if the strings are equal (case-insensitive); otherwise, <c>false</c>.</returns>
        public static bool IsTheSameAs(this string target, string stringToCompare)
        {
            return IsTheSameAs(target, stringToCompare, true);
        }

        /// <summary>
        /// Compares two strings for equality with optional case sensitivity.
        /// </summary>
        /// <param name="target">The first string.</param>
        /// <param name="stringToCompare">The string to compare against.</param>
        /// <param name="ignoreCase">If <c>true</c>, performs a case-insensitive comparison.</param>
        /// <returns><c>true</c> if the strings are equal; otherwise, <c>false</c>.</returns>
        public static bool IsTheSameAs(this string target, string stringToCompare, bool ignoreCase)
        {
            return string.Compare(target, stringToCompare, ignoreCase) == 0;
        }

        /// <summary>
        /// Transforms the string into a URL-friendly slug
        /// </summary>
        /// <param name="name">The original string</param>
        /// <returns>A string containing a url-friendly slug</returns>
        public static string ToSlug(this string name)
        {
            var sb = new StringBuilder();
            string lower = string.IsNullOrEmpty(name) ? "" : name.ToLower();
            foreach (char c in lower)
            {
                if (c == ' ' || c == '.' || c == '=' || c == '-')
                    sb.Append('-');
                else if ((c <= 'z' && c >= 'a') || (c <= '9' && c >= '0'))
                    sb.Append(c);
            }

            return sb.ToString().Trim('-');
        }

        /// <summary>
        /// Returns the plural form of the string using the <see cref="Inflector"/>.
        /// </summary>
        /// <param name="target">The word to pluralize.</param>
        /// <returns>The pluralized form of the word.</returns>
        /// <seealso cref="Inflector.Pluralize(string)"/>
        public static string Pluralize(this string target)
        {
            return Inflector.Pluralize(target);
        }

        /// <summary>
        /// Conditionally pluralizes the string based on the result of a boolean expression.
        /// </summary>
        /// <param name="target">The word to conditionally pluralize.</param>
        /// <param name="expression">An expression that returns <c>true</c> if the word should be pluralized.</param>
        /// <returns>The pluralized form if the expression evaluates to <c>true</c>; otherwise, the original string.</returns>
        public static string PluralizeIf(this string target, Expression<Func<bool>> expression)
        {
            if (expression.Invoke())
            {
                return Inflector.Pluralize(target);
            }
            else
            {
                return target;
            }
        }

        /// <summary>
        /// Adds a char to end of given string if it does not ends with the char.
        /// </summary>
        public static string EnsureEndsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Guard.IsNotNull(str, nameof(str));

            if (str.EndsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return str + c;
        }

        /// <summary>
        /// Adds a char to beginning of given string if it does not starts with the char.
        /// </summary>
        public static string EnsureStartsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Guard.IsNotNull(str, nameof(str));

            if (str.StartsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return c + str;
        }

        /// <summary>
        /// Indicates whether this string is null or an System.String.Empty string.
        /// </summary>
        public static bool IsNullOrEmpty(this string? str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
        public static string Left(this string str, int len)
        {
            Guard.IsNotNull(str, nameof(str));

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length!");
            }

            return str.Substring(0, len);
        }

        /// <summary>
        /// Converts line endings in the string to <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Gets index of nth occurrence of a char in a string.
        /// </summary>
        /// <param name="str">source string to be searched</param>
        /// <param name="c">Char to search in <paramref name="str"/></param>
        /// <param name="n">Count of the occurrence</param>
        public static int NthIndexOf(this string str, char c, int n)
        {
            Guard.IsNotNull(str, nameof(str));

            var count = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != c)
                {
                    continue;
                }

                if ((++count) == n)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes first occurrence of the given postfixes from end of the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="postFixes">one or more postfix.</param>
        /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
        public static string RemovePostFix(this string str, params string[] postFixes)
        {
            return str.RemovePostFix(StringComparison.Ordinal, postFixes);
        }

        /// <summary>
        /// Removes first occurrence of the given postfixes from end of the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="comparisonType">String comparison type</param>
        /// <param name="postFixes">one or more postfix.</param>
        /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
        public static string RemovePostFix(this string str, StringComparison comparisonType, params string[] postFixes)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (postFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var postFix in postFixes)
            {
                if (str.EndsWith(postFix, comparisonType))
                {
                    return str.Left(str.Length - postFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// Removes first occurrence of the given prefixes from beginning of the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="preFixes">one or more prefix.</param>
        /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
        public static string RemovePreFix(this string str, params string[] preFixes)
        {
            return str.RemovePreFix(StringComparison.Ordinal, preFixes);
        }

        /// <summary>
        /// Removes first occurrence of the given prefixes from beginning of the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="comparisonType">String comparison type</param>
        /// <param name="preFixes">one or more prefix.</param>
        /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
        public static string RemovePreFix(this string str, StringComparison comparisonType, params string[] preFixes)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (preFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var preFix in preFixes)
            {
                if (str.StartsWith(preFix, comparisonType))
                {
                    return str.Right(str.Length - preFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// Replaces the first occurrence of a search string with a replacement string.
        /// </summary>
        /// <param name="str">The source string.</param>
        /// <param name="search">The string to find.</param>
        /// <param name="replace">The replacement string.</param>
        /// <param name="comparisonType">The string comparison type to use for finding the search string.</param>
        /// <returns>The string with the first occurrence replaced, or the original string if not found.</returns>
        public static string ReplaceFirst(this string str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Guard.IsNotNull(str, nameof(str));

            var pos = str.IndexOf(search, comparisonType);
            if (pos < 0)
            {
                return str;
            }

            return str.Substring(0, pos) + replace + str.Substring(pos + search.Length);
        }

        /// <summary>
        /// Gets a substring of a string from end of the string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
        public static string Right(this string str, int len)
        {
            Guard.IsNotNull(str, nameof(str));

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length!");
            }

            return str.Substring(str.Length - len, len);
        }

        /// <summary>
        /// Uses string.Split method to split given string by given separator.
        /// </summary>
        public static string[] Split(this string str, string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// Uses string.Split method to split given string by given separator.
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options)
        {
            return str.Split(new[] { separator }, options);
        }

        /// <summary>
        /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string[] SplitToLines(this string str)
        {
            return str.Split(Environment.NewLine);
        }

        /// <summary>
        /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
        /// </summary>
        public static string[] SplitToLines(this string str, StringSplitOptions options)
        {
            return str.Split(Environment.NewLine, options);
        }

        /// <summary>
        /// Converts PascalCase string to camelCase string.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
        /// <param name="handleAbbreviations">set true to if you want to convert 'XYZ' to 'xyz'.</param>
        /// <returns>camelCase of the string</returns>
        public static string ToCamelCase(this string str, bool useCurrentCulture = false, bool handleAbbreviations = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
            }

            if (handleAbbreviations && IsAllUpperCase(str))
            {
                return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
            }

            return (useCurrentCulture ? char.ToLower(str[0]) : char.ToLowerInvariant(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
        /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
        public static string ToSentenceCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return useCurrentCulture
                ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
                : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to kebab-case.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
        public static string ToKebabCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            str = str.ToCamelCase();

            return useCurrentCulture
                ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLower(m.Value[1]))
                : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
        }

        /// <summary>
        /// Converts given PascalCase/camelCase string to snake case.
        /// Example: "ThisIsSampleSentence" is converted to "this_is_a_sample_sentence".
        /// https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/NameTranslation/NpgsqlSnakeCaseNameTranslator.cs#L51
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns></returns>
        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            var builder = new StringBuilder(str.Length + Math.Min(2, str.Length / 5));
            var previousCategory = default(UnicodeCategory?);

            for (var currentIndex = 0; currentIndex < str.Length; currentIndex++)
            {
                var currentChar = str[currentIndex];
                if (currentChar == '_')
                {
                    builder.Append('_');
                    previousCategory = null;
                    continue;
                }

                var currentCategory = char.GetUnicodeCategory(currentChar);
                switch (currentCategory)
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                        if (previousCategory == UnicodeCategory.SpaceSeparator ||
                            previousCategory == UnicodeCategory.LowercaseLetter ||
                            previousCategory != UnicodeCategory.DecimalDigitNumber &&
                            previousCategory != null &&
                            currentIndex > 0 &&
                            currentIndex + 1 < str.Length &&
                            char.IsLower(str[currentIndex + 1]))
                        {
                            builder.Append('_');
                        }

                        currentChar = char.ToLower(currentChar);
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        if (previousCategory == UnicodeCategory.SpaceSeparator)
                        {
                            builder.Append('_');
                        }
                        break;

                    default:
                        if (previousCategory != null)
                        {
                            previousCategory = UnicodeCategory.SpaceSeparator;
                        }
                        continue;
                }

                builder.Append(currentChar);
                previousCategory = currentCategory;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts string to enum value.
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="value">String value to convert</param>
        /// <returns>Returns enum object</returns>
        public static T ToEnum<T>(this string value)
            where T : struct
        {
            Guard.IsNotNull(value, nameof(value));
            return (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>
        /// Converts string to enum value.
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="value">String value to convert</param>
        /// <param name="ignoreCase">Ignore case</param>
        /// <returns>Returns enum object</returns>
        public static T ToEnum<T>(this string value, bool ignoreCase)
            where T : struct
        {
            Guard.IsNotNull(value, nameof(value));
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        /// <summary>
        /// Computes the MD5 hash of the string and returns it as an uppercase hexadecimal string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The MD5 hash as an uppercase hexadecimal string.</returns>
        public static string ToMd5(this string str)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(str);
                var hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                foreach (var hashByte in hashBytes)
                {
                    sb.Append(hashByte.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts camelCase string to PascalCase string.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="useCurrentCulture">set true to use current culture. Otherwise, invariant culture will be used.</param>
        /// <returns>PascalCase of the string</returns>
        public static string ToPascalCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return useCurrentCulture ? str.ToUpper() : str.ToUpperInvariant();
            }

            return (useCurrentCulture ? char.ToUpper(str[0]) : char.ToUpperInvariant(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string? Truncate(this string? str, int maxLength)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            return str.Left(maxLength);
        }

        /// <summary>
        /// Gets a substring of a string from Ending of the string if it exceeds maximum length.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string? TruncateFromBeginning(this string? str, int maxLength)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            return str.Right(maxLength);
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// It adds a "..." postfix to end of the string if it's truncated.
        /// Returning string can not be longer than maxLength.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string? TruncateWithPostfix(this string? str, int maxLength)
        {
            return TruncateWithPostfix(str, maxLength, "...");
        }

        /// <summary>
        /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
        /// It adds given <paramref name="postfix"/> to end of the string if it's truncated.
        /// Returning string can not be longer than maxLength.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
        public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix)
        {
            if (str == null)
            {
                return null;
            }

            if (str == string.Empty || maxLength == 0)
            {
                return string.Empty;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            if (maxLength <= postfix.Length)
            {
                return postfix.Left(maxLength);
            }

            return str.Left(maxLength - postfix.Length) + postfix;
        }

        /// <summary>
        /// Converts given string to a byte array using <see cref="Encoding.UTF8"/> encoding.
        /// </summary>
        public static byte[] GetBytes(this string str)
        {
            return str.GetBytes(Encoding.UTF8);
        }

        /// <summary>
        /// Converts given string to a byte array using the given <paramref name="encoding"/>
        /// </summary>
        public static byte[] GetBytes(this string str,  Encoding encoding)
        {
            Guard.IsNotNull(str, nameof(str));
            Guard.IsNotNull(encoding, nameof(encoding));

            return encoding.GetBytes(str);
        }

        /// <summary>
        /// Determines whether all letter characters in the input string are uppercase.
        /// Non-letter characters are ignored.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns><c>true</c> if all letter characters are uppercase; otherwise, <c>false</c>.</returns>
        private static bool IsAllUpperCase(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
