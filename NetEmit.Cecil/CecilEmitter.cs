using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using NetEmit.API;

namespace NetEmit.Cecil
{
    public class CecilEmitter : IAssemblyEmitter
    {
        public void Emit(IAssembly ass)
        {
            var ver = Version.Parse(ass.GetVersion());
            var assName = new AssemblyNameDefinition(ass.Name, ver)
            {
                HashAlgorithm = AssemblyHashAlgorithm.SHA1
            };
            var moduleName = ass.GetFileName();
            var file = Path.GetFullPath(ass.GetFileName());
            using (var resolver = new DefaultAssemblyResolver())
            {
                var dir = Path.GetDirectoryName(file);
                resolver.AddSearchDirectory(dir);
                var parms = new ModuleParameters
                {
                    Kind = ass.IsExe ? ModuleKind.Console : ModuleKind.Dll,
                    Runtime = TargetRuntime.Net_4_0,
                    AssemblyResolver = resolver
                };
                using (var dyn = AssemblyDefinition.CreateAssembly(assName, moduleName, parms))
                {
                    Emit(ass, dyn);
                    var wparms = new WriterParameters {WriteSymbols = false};
                    dyn.Write(file, wparms);
                }
            }

            Console.WriteLine(file);
        }

        private static void Emit(IAssembly ass, AssemblyDefinition bld)
        {
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(true)
            );
        }

        public void Dispose()
        {
        }
    }
}