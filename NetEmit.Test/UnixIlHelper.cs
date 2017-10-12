using System;

namespace NetEmit.Test
{
    public class UnixIlHelper : ILHelper
    {
        public string Escape(string path) => '"' + path + '"';

        public Tuple<string, string> GetDasmCmd(string file, string il)
            => Tuple.Create("monodis", $"--output={Escape(il)} {Escape(file)}");
    }
}