using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace Kuuhaku.BooruModule
{
    public class CustomTitleCaseTransformer : IStringTransformer
    {
        private static IStringTransformer _instance;

        public static IStringTransformer Instance =>
            _instance ?? (_instance = new CustomTitleCaseTransformer());

        public String Transform(String input)
        {
            var words = input.Replace("_", " ").Split(' ');
            var transformed = new List<String>();

            foreach (var s in words)
            {
                if (s.Length == 0 || this.IsAllCapitals(s))
                    transformed.Add(s);
                if (s.Length == 1)
                    transformed.Add(s.ToUpperInvariant());

                var prefix = "";
                var newWord = s;

                if (s[0] == '(' && s.Length >= 2)
                {
                    prefix = "(";
                    newWord = s.Remove(0, 1);
                }

                transformed.Add(prefix + Char.ToUpper(newWord[0]) + newWord.Remove(0, 1).ToLower());
            }

            return String.Join(" ", transformed);
        }

        private Boolean IsAllCapitals(String input)
            => input.ToCharArray().All(Char.IsUpper);
    }
}
