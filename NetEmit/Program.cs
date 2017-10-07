using NetEmit.API;
using NetEmit.CodeDOM;
using NetEmit.Netfx;

namespace NetEmit
{
    public class Program
    {
        public static void Main()
        {
            using (IAssemblyEmitter bld = new AssemblyEmitter())
            {
                IAssembly ass = new NewAssembly
                {
                    Name = "TestGen",
                    FileName = "TestGen1.dll"
                };
                bld.Emit(ass);
            }

            using (IAssemblyEmitter bld = new CSharpEmitter())
            {
                IAssembly ass = new NewAssembly
                {
                    Name = "TestGen",
                    FileName = "TestGen2.dll"
                };
                bld.Emit(ass);
            }

            // Console.ReadLine();
        }
    }
}