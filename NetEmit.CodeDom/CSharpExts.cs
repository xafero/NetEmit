using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace NetEmit.CodeDom
{
    public static class CSharpExts
    {
        public static string ToCode(this bool value) => value.ToString().ToLowerInvariant();

        public static Assembly TryGetCompiledAssembly(this CompilerResults res)
        {
            try
            {
                return res.CompiledAssembly;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}