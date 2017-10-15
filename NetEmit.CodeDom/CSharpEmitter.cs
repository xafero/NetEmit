using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public string Emit(AssemblyDef ass)
        {
            var file = Path.GetFullPath(ass.GetFileName());
            var parms = new CompilerParameters
            {
                CompilerOptions = $"/target:{ass.GetKind()} /optimize",
                GenerateExecutable = ass.IsExe(),
                IncludeDebugInformation = false,
                OutputAssembly = file
            };
            var sources = new List<string> { GenerateMeta(ass) };
            sources.AddRange(GenerateCode(ass));
            WriteAllCode(file, sources);
            var results = Provider.CompileAssemblyFromSource(parms, sources.ToArray());
            var dyn = results.TryGetCompiledAssembly();
            if (dyn == null)
                throw new InvalidOperationException(ToText(results));
            return Path.GetFullPath(results.PathToAssembly ?? dyn.Location ?? dyn.CodeBase);
        }

        private static void WriteAllCode(string file, IEnumerable<string> sources)
        {
            var code = string.Join(Environment.NewLine, sources);
            var csDir = Path.GetDirectoryName(file) ?? string.Empty;
            var csFile = $"{Path.GetFileNameWithoutExtension(file) ?? "gen"}.cs";
            var csPath = Path.Combine(csDir, csFile);
            File.WriteAllText(csPath, code, Encoding.UTF8);
        }

        private static string GenerateMeta(AssemblyDef ass)
        {
            var code = new StringWriter();
            code.WriteLine("using System;");
            code.WriteLine("using System.Reflection;");
            code.WriteLine("using System.Runtime.CompilerServices;");
            code.WriteLine();
            code.WriteLine($@"[assembly: CompilationRelaxations({(int)ass.GetRelaxations()})]");
            code.WriteLine("[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = "
                + $"{ass.ShouldWrapNonExceptions().ToCode()})]");
            code.WriteLine($@"[assembly: AssemblyVersion(""{ass.GetVersion()}"")]");
            return code.ToString();
        }

        private static IEnumerable<string> GenerateCode(AssemblyDef ass)
        {
            foreach (var nsp in ass.GetNamespaces())
            {
                var n = N.Create<NA.INamespace>(nsp.Name);
                foreach (var type in nsp.Types)
                    GenerateType(n, type);
                yield return n.ToString();
            }
        }

        private static void GenerateType(NA.INamespace nsp, TypeDef typ)
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

        private static void EmitClass(NA.INamespace nsp, TypeDef typ)
        {
            var c = N.Create<NA.IClass>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitInterface(NA.INamespace nsp, TypeDef typ)
        {
            var i = N.Create<NA.IInterface>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitDelegate(NA.INamespace nsp, TypeDef typ)
        {
            var d = N.Create<NA.IDelegate>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitStruct(NA.INamespace nsp, TypeDef typ)
        {
            var s = N.Create<NA.IStruct>(typ.Name, nsp).With(NA.Visibility.Public);
        }

        private static void EmitEnum(NA.INamespace nsp, TypeDef typ)
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