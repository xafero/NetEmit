using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetEmit.Test
{
    internal interface ICilNormalizer
    {
        string Normalize(string text);
    }

    internal class CilSortNormalizer : ICilNormalizer
    {
        private readonly Regex _prop;
        private readonly Regex _get;
        private readonly Regex _set;
        private readonly Regex _evt;
        private readonly Regex _add;
        private readonly Regex _rem;

        public CilSortNormalizer()
        {
            _prop = CreateRegex("\\.property.*?(?=})");
            _get = CreateRegex("\\.get.*?(?=\\))");
            _set = CreateRegex("\\.set.*?(?=\\))");
            _evt = CreateRegex("\\.event.*?(?=})");
            _add = CreateRegex("\\.addon.*?(?=\\))");
            _rem = CreateRegex("\\.removeon.*?(?=\\))");
        }

        public string Normalize(string text)
        {
            text = NormalizeBlock(text, _prop, _get, _set);
            text = NormalizeBlock(text, _evt, _add, _rem);
            return text;
        }

        private static IEnumerable<Match> Matches(Regex regex, string input)
            => regex.Matches(input).OfType<Match>().ToArray();

        private static Regex CreateRegex(string regex)
        {
            const RegexOptions opts = RegexOptions.Singleline | RegexOptions.CultureInvariant;
            return new Regex(regex, opts);
        }

        private static string NormalizeBlock(string text, Regex root, Regex get, Regex set)
        {
            var props = Matches(root, text);
            foreach (var item in props)
            {
                var itemTxt = item.Value;
                var gets = Matches(get, itemTxt).SingleOrDefault();
                var sets = Matches(set, itemTxt).SingleOrDefault();
                if (gets == null || sets == null || sets.Index >= gets.Index)
                    continue;
                text = Replace(text, item.Index, sets, gets);
            }
            return text;
        }

        private static string Replace(string text, int offset, Capture first, Capture second)
        {
            text = text.Remove(offset + second.Index, second.Length + 1 + 2 + 2);
            text = text.Remove(offset + first.Index, first.Length + 1 + 2 + 2);
            text = text.Insert(offset + first.Index, second.Value + ")" + Environment.NewLine +
                "    " + first.Value + ")" + Environment.NewLine);
            return text;
        }
    }
}