using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EdFi.LoadTools.Engine
{
    public static class ExtensionMethods
    {
        public static string InitialUpperCase(this string text)
        {
            return text.Substring(0, 1).ToUpperInvariant() + text.Substring(1);
        }

        public static bool AreMatchingSimpleTypes(string jsonType, string xmlType)
        {
            return Constants.AtomicTypes.Any(x => x.Json == jsonType && x.Xml == xmlType);
        }

        /// <summary>
        /// This extension method implements a string comparison algorithm
        /// based on character pair similarity "shingling"
        /// Source: http://www.hpl.hp.com/techreports/Compaq-DEC/SRC-TN-1997-015.pdf
        /// </summary>
        public static double PercentMatchTo(this string str1, string str2)
        {
            var pairs1 = WordLetterPairs(str1.InitialUpperCase());
            var pairs2 = WordLetterPairs(str2.InitialUpperCase());

            var intersection = 0;
            var union = pairs1.Concat(pairs2).Distinct().Count();

            foreach (var t in pairs1)
            {
                for (var j = 0; j < pairs2.Count; j++)
                {
                    if (t != pairs2[j]) continue;
                    intersection++;
                    pairs2.RemoveAt(j); //Must remove the match to prevent "GGGG" from appearing to match "GG" with 100% success
                    break;
                }
            }
            // always return something more than zero
            return Math.Max(1.0 * intersection / union, 0.000001D);
        }

        /// <summary>
        /// Gets all letter pairs for each
        /// individual word in the string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static List<string> WordLetterPairs(string str)
        {
            var allPairs = new List<string>();
            
            // Tokenize the string and put the tokens/words into an array
            var words = Regex.Split(str, @"[\s/]");

            // For each word
            foreach (var pairsInWord in from t in words where !string.IsNullOrEmpty(t) select LetterPairs(t))
            {
                allPairs.AddRange(pairsInWord);
            }
            return allPairs;
        }

        /// <summary>
        /// Generates an array containing every 
        /// two consecutive letters in the input string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static IEnumerable<string> LetterPairs(string str)
        {
            var numPairs = str.Length - 1;
            var pairs = new string[numPairs];
            for (var i = 0; i < numPairs; i++)
            {
                pairs[i] = str.Substring(i, 2);
            }
            return pairs;
        }
    }
}
