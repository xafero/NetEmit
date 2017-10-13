using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NetEmit.Test
{
    public class UnixIlHelper : ILHelper
    {
        public string Escape(string path) => '"' + path + '"';

        public Tuple<string, string> GetDasmCmd(string file, string il)
            => Tuple.Create("monodis", $"--output={Escape(il)} {Escape(file)}");

        private static readonly Encoding E = Encoding.UTF8;

        public void Filter(string file)
            => File.WriteAllLines(file, File.ReadAllLines(file, E).Where(IsNeeded).Select(Correct), E);

        private static string Correct(string line)
            => line.Split(new[] {" // GUID = {"}, StringSplitOptions.None).First();

        private static bool IsNeeded(string line)
            => new[]
            {
                "// Method begins at RVA 0x", ".pack ", ".size "
            }.All(l => !line.Contains(l));
    }
}