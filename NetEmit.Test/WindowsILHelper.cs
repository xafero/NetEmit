using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NetEmit.Test
{
    public class WindowsIlHelper : ILHelper
    {
        public string LocateDasm()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft SDKs", "Windows", "v10.0A", "bin", "NETFX 4.6.1 Tools", "ildasm.exe");

        public string Escape(string path) => '"' + path + '"';

        public Tuple<string, string> GetDasmCmd(string file, string il)
            => Tuple.Create($"{Escape(LocateDasm())}", $"{Escape(file)} /out={Escape(il)} /utf8 /source");

        private static readonly Encoding E = Encoding.UTF8;

        public void Filter(string file)
            => File.WriteAllLines(file, File.ReadAllLines(file, E).Where(IsNeeded), E);

        private static bool IsNeeded(string line)
            => new[]
                {
                    "// MVID:", "// Image base:", ".imagebase 0x", ".maxstack  ",
                    ".pack ", ".size ", "// Warnung: ", "System.Diagnostics.DebuggableAttribute",
                    "added automatically, do not uncomment ---"
                }.All(l => !line.Contains(l));
    }
}