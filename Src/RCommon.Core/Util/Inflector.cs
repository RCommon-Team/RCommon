

/*
 * CREDIT -  Originaly adapted from Inflector.Net (http://andrewpeters.net/inflectornet/)
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RCommon.Util
{
    ///<summary>
    /// Implementation of the Infelctor in Ruby that transforms words from singular to plural, 
    /// class names to table names, modularized class names to ones without, and class names to foreign keys
    ///</summary>
    public static class Inflector
    {
        /// <summary>Collection of pluralization rules applied in reverse order.</summary>
        private static readonly List<Rule> Plurals = new List<Rule>();
        /// <summary>Collection of singularization rules applied in reverse order.</summary>
        private static readonly List<Rule> Singulars = new List<Rule>();
        /// <summary>Collection of words that are the same in both singular and plural forms.</summary>
        private static readonly List<string> Uncountables = new List<string>();

        /// <summary>
        /// Class Constructor.
        /// </summary>
        static Inflector()
        {
            AddPlural("$", "s");
            AddPlural("s$", "s");
            AddPlural("(ax|test)is$", "$1es");
            AddPlural("(octop|vir)us$", "$1i");
            AddPlural("(alias|status)$", "$1es");
            AddPlural("(bu)s$", "$1ses");
            AddPlural("(buffal|tomat)o$", "$1oes");
            AddPlural("([ti])um$", "$1a");
            AddPlural("sis$", "ses");
            AddPlural("(?:([^f])fe|([lr])f)$", "$1$2ves");
            AddPlural("(hive)$", "$1s");
            AddPlural("([^aeiouy]|qu)y$", "$1ies");
            AddPlural("(x|ch|ss|sh)$", "$1es");
            AddPlural("(matr|vert|ind)ix|ex$", "$1ices");
            AddPlural("([m|l])ouse$", "$1ice");
            AddPlural("^(ox)$", "$1en");
            AddPlural("(quiz)$", "$1zes");

            AddSingular("s$", "");
            AddSingular("(n)ews$", "$1ews");
            AddSingular("([ti])a$", "$1um");
            AddSingular("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
            AddSingular("(^analy)ses$", "$1sis");
            AddSingular("([^f])ves$", "$1fe");
            AddSingular("(hive)s$", "$1");
            AddSingular("(tive)s$", "$1");
            AddSingular("([lr])ves$", "$1f");
            AddSingular("([^aeiouy]|qu)ies$", "$1y");
            AddSingular("(s)eries$", "$1eries");
            AddSingular("(m)ovies$", "$1ovie");
            AddSingular("(x|ch|ss|sh)es$", "$1");
            AddSingular("([m|l])ice$", "$1ouse");
            AddSingular("(bus)es$", "$1");
            AddSingular("(o)es$", "$1");
            AddSingular("(shoe)s$", "$1");
            AddSingular("(cris|ax|test)es$", "$1is");
            AddSingular("(octop|vir)i$", "$1us");
            AddSingular("(alias|status)es$", "$1");
            AddSingular("^(ox)en", "$1");
            AddSingular("(vert|ind)ices$", "$1ex");
            AddSingular("(matr)ices$", "$1ix");
            AddSingular("(quiz)zes$", "$1");

            AddIrregular("person", "people");
            AddIrregular("man", "men");
            AddIrregular("child", "children");
            AddIrregular("sex", "sexes");
            AddIrregular("move", "moves");

            AddUncountable("equipment");
            AddUncountable("information");
            AddUncountable("rice");
            AddUncountable("money");
            AddUncountable("species");
            AddUncountable("series");
            AddUncountable("fish");
            AddUncountable("sheep");
        }

        /// <summary>
        /// Represents a regex-based inflection rule that transforms words via pattern matching and replacement.
        /// </summary>
        private class Rule
        {
            private readonly Regex _regex;
            private readonly string _replacement;

            /// <summary>
            /// Initializes a new inflection rule with the specified regex pattern and replacement string.
            /// </summary>
            /// <param name="pattern">The regex pattern to match against words.</param>
            /// <param name="replacement">The replacement string (may include regex group references like $1).</param>
            public Rule(string pattern, string replacement)
            {
                _regex = new Regex(pattern, RegexOptions.IgnoreCase);
                _replacement = replacement;
            }

            /// <summary>
            /// Applies this rule to a word. Returns the transformed word if the pattern matches, or null otherwise.
            /// </summary>
            /// <param name="word">The word to transform.</param>
            /// <returns>The transformed word, or null if the pattern does not match.</returns>
            public string? Apply(string word)
            {
                if (!_regex.IsMatch(word))
                {
                    return null;
                }

                return _regex.Replace(word, _replacement);
            }
        }

        /// <summary>
        /// Adds bidirectional rules for an irregular word (e.g., "person" / "people"),
        /// preserving the first letter's case.
        /// </summary>
        /// <param name="singular">The singular form of the irregular word.</param>
        /// <param name="plural">The plural form of the irregular word.</param>
        private static void AddIrregular(string singular, string plural)
        {
            AddPlural("(" + singular[0] + ")" + singular.Substring(1) + "$", "$1" + plural.Substring(1));
            AddSingular("(" + plural[0] + ")" + plural.Substring(1) + "$", "$1" + singular.Substring(1));
        }

        /// <summary>
        /// Registers a word as uncountable (same in singular and plural forms, e.g., "sheep").
        /// </summary>
        /// <param name="word">The uncountable word to register.</param>
        private static void AddUncountable(string word)
        {
            Uncountables.Add(word.ToLower());
        }

        /// <summary>
        /// Adds a pluralization regex rule.
        /// </summary>
        /// <param name="rule">The regex pattern for matching singular words.</param>
        /// <param name="replacement">The replacement pattern for creating the plural form.</param>
        private static void AddPlural(string rule, string replacement)
        {
            Plurals.Add(new Rule(rule, replacement));
        }

        /// <summary>
        /// Adds a singularization regex rule.
        /// </summary>
        /// <param name="rule">The regex pattern for matching plural words.</param>
        /// <param name="replacement">The replacement pattern for creating the singular form.</param>
        private static void AddSingular(string rule, string replacement)
        {
            Singulars.Add(new Rule(rule, replacement));
        }

        /// <summary>
        /// Applies the given list of rules to a word in reverse order (last rule wins).
        /// Words in the <see cref="Uncountables"/> list are returned unchanged.
        /// </summary>
        /// <param name="rules">The list of rules to apply.</param>
        /// <param name="word">The word to transform.</param>
        /// <returns>The transformed word, or the original word if no rule matches or the word is uncountable.</returns>
        private static string ApplyRules(List<Rule> rules, string word)
        {
            string result = word;

            if (!Uncountables.Contains(word.ToLower()))
            {
                for (int i = rules.Count - 1; i >= 0; i--)
                {
                    string? applied = rules[i].Apply(word);
                    if (applied != null)
                    {
                        result = applied;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the plural form of the word in the string
        /// </summary>
        /// <param name="word">string. The word to pluralize.</param>
        /// <returns>The pluralized word.</returns>
        public static string Pluralize(string word)
        {
            return ApplyRules(Plurals, word);
        }

        /// <summary>
        /// The reverse of <see cref="Pluralize"/>, returns the singular form of a word in a string.
        /// </summary>
        /// <param name="word">string. The word to singularize.</param>
        /// <returns>The singluralized word.</returns>
        public static string Singularize(string word)
        {
            return ApplyRules(Singulars, word);
        }

        /// <summary>
        /// Capitalizes all the words and replaces some characters in the string to create a nicer looking title.
        /// </summary>
        /// <param name="word">string. The word to titleize.</param>
        /// <returns>The titlized word.</returns>
        public static string Titleize(string word)
        {
            return Regex.Replace(Humanize(Underscore(word)), @"\b([a-z])",
                                 delegate(Match match)
                                 {
                                     return match.Captures[0].Value.ToUpper();
                                 });
        }

        /// <summary>
        /// Capitalizes the first word and turns underscores into spaces and strips _id. Formats the word into
        /// human readable string.
        /// </summary>
        /// <param name="lowercaseAndUnderscoredWord">string. The word to humaize</param>
        /// <returns>The humanized word.</returns>
        public static string Humanize(string lowercaseAndUnderscoredWord)
        {
            return Capitalize(Regex.Replace(lowercaseAndUnderscoredWord, @"_", " "));
        }

        /// <summary>
        /// Formats the string in pascal case.
        /// </summary>
        /// <param name="lowercaseAndUnderscoredWord">string. The word to Pascal case.</param>
        /// <returns>The word in Pascal case.</returns>
        public static string Pascalize(string lowercaseAndUnderscoredWord)
        {
            return Regex.Replace(lowercaseAndUnderscoredWord, "(?:^|_)(.)",
                                 delegate(Match match)
                                 {
                                     return match.Groups[1].Value.ToUpper();
                                 });
        }

        /// <summary>
        /// Formats the string in Camel case.
        /// </summary>
        /// <param name="lowercaseAndUnderscoredWord">string. The word to format in Camel case.</param>
        /// <returns>string. The word in Camel case.</returns>
        public static string Camelize(string lowercaseAndUnderscoredWord)
        {
            return Uncapitalize(Pascalize(lowercaseAndUnderscoredWord));
        }

        /// <summary>
        /// Makes an underscored form from the expression in the string. 
        /// </summary>
        /// <param name="pascalCasedWord">string. The word to underscore.</param>
        /// <returns>string. The word with underscore seperators.</returns>
        public static string Underscore(string pascalCasedWord)
        {
            return Regex.Replace(
              Regex.Replace(
                Regex.Replace(pascalCasedWord, @"([A-Z]+)([A-Z][a-z])", "$1_$2"), @"([a-z\d])([A-Z])",
                "$1_$2"), @"[-\s]", "_").ToLower();
        }

        /// <summary>
        /// Capitalizes the word.
        /// </summary>
        /// <param name="word">string. The word to capitalize.</param>
        /// <returns>The Capitalized word.</returns>
        public static string Capitalize(string word)
        {
            return word.Substring(0, 1).ToUpper() + word.Substring(1).ToLower();
        }

        /// <summary>
        /// Revers of <see cref="Capitalize"/>
        /// </summary>
        /// <param name="word">string. The word to un-capitalize.</param>
        /// <returns></returns>
        public static string Uncapitalize(string word)
        {
            return word.Substring(0, 1).ToLower() + word.Substring(1);
        }

        /// <summary>
        /// Ordinalize turns a number into an ordinal string used to denote the position in an ordered 
        /// sequence such as 1st, 2nd, 3rd, 4th
        /// </summary>
        /// <param name="number">string. The number to ordinalize.</param>
        /// <returns>string. The ordinalized number.</returns>
        public static string Ordinalize(string number)
        {
            int n = int.Parse(number);
            int nMod100 = n % 100;

            if (nMod100 >= 11 && nMod100 <= 13)
            {
                return number + "th";
            }

            switch (n % 10)
            {
                case 1:
                    return number + "st";
                case 2:
                    return number + "nd";
                case 3:
                    return number + "rd";
                default:
                    return number + "th";
            }
        }

        /// <summary>
        /// Replaces underscores with dashes in the string.
        /// </summary>
        /// <param name="underscoredWord">string. The word to dasherize.</param>
        /// <returns>The word with dashes instead of underscores.</returns>
        public static string Dasherize(string underscoredWord)
        {
            return underscoredWord.Replace('_', '-');
        }
    }
}