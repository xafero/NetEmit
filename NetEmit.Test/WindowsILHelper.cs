using System;
using System.IO;

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
    }
}