using System;

namespace NetEmit.Test
{
    public interface ILHelper
    {
        Tuple<string, string> GetDasmCmd(string file, string il);
    }
}