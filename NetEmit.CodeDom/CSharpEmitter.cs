using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using NetEmit.API;

namespace NetEmit.CodeDom
{    
    public class CSharpEmitter : IAssemblyEmitter
    {
        public CSharpCodeProvider Provider { get; }

        public CSharpEmitter()
        {
            Provider = new CSharpCodeProvider();
        }

        public void Emit(IAssembly ass)
        {
            var file = Path.GetFullPath(ass.GetFileName());
            var parms = new CompilerParameters
            {
                CompilerOptions = $"/target:{ass.GetKind()} /optimize",
                GenerateExecutable = ass.IsExe,
                IncludeDebugInformation = false,
                OutputAssembly = file
            };
            var sources = new List<string> { GenerateMeta(ass) };
            var results = Provider.CompileAssemblyFromSource(parms, sources.ToArray());
            var dyn = results.CompiledAssembly;
            if (dyn == null)
                throw new InvalidOperationException(ToText(results));
            var path = Path.GetFullPath(dyn.Location ?? dyn.CodeBase);

            Console.WriteLine(path);
        }

        private static string GenerateMeta(IAssembly ass)
        {
            var code = new StringWriter();
            code.WriteLine("using System;");
            code.WriteLine("using System.Reflection;");
            code.WriteLine();
            code.WriteLine($@"[assembly: AssemblyVersion(""{ass.Version}"")]");
            return code.ToString();
        }

        private static string ToText(CompilerResults results)
            => string.Join(Environment.NewLine, results.Errors.OfType<CompilerError>());

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}