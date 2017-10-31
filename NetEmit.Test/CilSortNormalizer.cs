using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly Regex _ass;
        private readonly Regex _cus;
        private readonly Regex _cla;
        private readonly Regex _fld;

        public CilSortNormalizer()
        {
            _prop = CreateRegex("\\.property.*?(?=})");
            _get = CreateRegex("\\.get.*?(?=\\))");
            _set = CreateRegex("\\.set.*?(?=\\))");
            _evt = CreateRegex("\\.event.*?(?=})");
            _add = CreateRegex("\\.addon.*?(?=\\))");
            _rem = CreateRegex("\\.removeon.*?(?=\\))");
            _ass = CreateRegex("\\.assembly.*?(?=})");
            _cus = CreateRegex("\\.custom instance.*?(?= \\))");
            _cla = CreateRegex("\\.class.*?(?=})");
            _fld = CreateRegex("(\\.field|\\.custom).*?(?=\\n)");
        }

        public string Normalize(string text)
        {
            text = NormalizeBlock(text, _prop, _get, _set);
            text = NormalizeBlock(text, _evt, _add, _rem);
            text = NormalizeList(text, _ass, _cus);
            text = NormalizeList(text, _cla, _fld);
            return text;
        }

        private static string NormalizeList(string text, Regex root, Regex entry)
        {
            var asses = Matches(root, text);
            foreach (var ass in asses)
            {
                var assTxt = ass.Value;
                var cuss = Matches(entry, assTxt).Select(m => new MyMatch(m, ass.Index, text)).ToList();
                if (!cuss.Any())
                    continue;
                text = Replace(text, ass.Index, cuss);
            }
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

        private static string Replace(string text, int offset, ICollection<MyMatch> matches)
        {
            var first = matches.First();
            var total = matches.Sum(m => m.Length + 2);
            text = text.Remove(offset + first.Index, total);
            var body = new StringBuilder();
            foreach (var match in matches.OrderBy(m => m.Value))
            {
                if (body.Length >= 1)
                    body.Append("  ");
                body.Append(match.Value);
            }
            text = text.Insert(offset + first.Index, body + "  ");
            return text;
        }

        private class MyMatch
        {
            public MyMatch(Capture m, int offset, string text)
            {
                var start = offset + m.Index;
                var lineFeed = text.IndexOf('\n', start + m.Length);
                var result = text.Substring(start, lineFeed - start) + '\n';
                Index = m.Index;
                Length = result.Length;
                Value = result;
            }

            public int Length { get; }
            public int Index { get; }
            public string Value { get; }

            public override string ToString() => $"({Index}+{Length}) {Value}";
        }
    }
}