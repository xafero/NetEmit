using NetEmit.API;
using NetEmit.Netfx;

namespace NetEmit
{
    public class Program
    {
        public static void Main()
        {
            using (IAssemblyEmitter bld = new AssemblyEmitter())
            {
                IAssembly ass = new NewAssembly { Name = "TestGen" };
                bld.Emit(ass);

            }

            // Console.ReadLine();
        }
    }
}
/*
 * CSharpCodeProvider provider = new CSharpCodeProvider();

CompilerParameters compilerParams = new CompilerParameters();

compilerParams.CompilerOptions = "/target:library /optimize";

compilerParams.GenerateExecutable = false;

compilerParams.GenerateInMemory = true;

compilerParams.IncludeDebugInformation = false;

 some ReferencedAssemblies

CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, dat);

Dll = results.CompiledAssembly;
    */
