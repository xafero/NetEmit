using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using NetEmit.API;
using Noaster.Dist;
using N = Noaster.Dist.Noaster;
using NA = Noaster.Api;

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
            sources.AddRange(GenerateCode(ass));
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
            code.WriteLine($@"[assembly: AssemblyVersion(""{ass.GetVersion()}"")]");
            return code.ToString();
        }

        private static IEnumerable<string> GenerateCode(IAssembly ass)
        {
            foreach (var nsp in ass.Namespaces)
            {
                var n = N.Create<NA.INamespace>(nsp.Name);
                foreach (var type in nsp.Types)
                    GenerateType(n, type);
                yield return n.ToString();
            }
        }

        private static void GenerateType(NA.INamespace nsp, IType typ)
        {
            switch (typ.Kind)
            {
                case TypeKind.Enum:
                    EmitEnum(nsp, typ);
                    break;
                case TypeKind.Struct:
                    EmitStruct(nsp, typ);
                    break;
                case TypeKind.Delegate:
                    EmitDelegate(nsp, typ);
                    break;
                case TypeKind.Interface:
                    EmitInterface(nsp, typ);
                    break;
                case TypeKind.Class:
                    EmitClass(nsp, typ);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(typ.Kind.ToString());
            }
        }

        private static void EmitClass(NA.INamespace nsp, IType typ)
        {
            var c = N.Create<NA.IClass>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitInterface(NA.INamespace nsp, IType typ)
        {
            var i = N.Create<NA.IInterface>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitDelegate(NA.INamespace nsp, IType typ)
        {
            var d = N.Create<NA.IDelegate>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitStruct(NA.INamespace nsp, IType typ)
        {
            var s = N.Create<NA.IStruct>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitEnum(NA.INamespace nsp, IType typ)
        {
            var e = N.Create<NA.IEnum>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static string ToText(CompilerResults results)
            => string.Join(Environment.NewLine, results.Errors.OfType<CompilerError>());

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}