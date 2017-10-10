using NetEmit.API;
using NetEmit.Cecil;
using NetEmit.CodeDom;
using NetEmit.Core;
using NetEmit.Netfx;

namespace NetEmit
{
    public class Program
    {
        public static void Main()
        {
            var nsp = new NewNamespace
            {
                Types =
                {
                    new NewEnum {Name = "MyE"},
                    new NewStruct {Name = "MyS"},
                    new NewDelegate {Name = "MyD"},
                    new NewInterface {Name = "MyI"},
                    new NewClass {Name = "MyC"}
                }
            };

            using (IAssemblyEmitter bld = new AssemblyEmitter())
            {
                IAssembly ass = new NewAssembly {Name = "TestGen1", Namespaces = {nsp}};
                bld.Emit(ass);
            }

            using (IAssemblyEmitter bld = new CSharpEmitter())
            {
                IAssembly ass = new NewAssembly {Name = "TestGen2", Namespaces = {nsp}};
                bld.Emit(ass);
            }

            using (IAssemblyEmitter bld = new CecilEmitter())
            {
                IAssembly ass = new NewAssembly {Name = "TestGen3", Namespaces = {nsp}};
                bld.Emit(ass);
            }

            // Console.ReadLine();
        }
    }
}