using NetEmit.API;
using NetEmit.Cecil;
using NetEmit.CodeDom;
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
                    Name = "TestGen1"
                };
                bld.Emit(ass);
            }

            using (IAssemblyEmitter bld = new CSharpEmitter())
            {
                IAssembly ass = new NewAssembly
                {
                    Name = "TestGen2"
                };
                bld.Emit(ass);
            }

            using (IAssemblyEmitter bld = new CecilEmitter())
            {
                IAssembly ass = new NewAssembly
                {
                    Name = "TestGen3"
                };
                bld.Emit(ass);
            }

            // Console.ReadLine();
        }
    }
}