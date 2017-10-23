using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
            var sources = new List<string> {GenerateMeta(ass)};
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
            var bld = new StringWriter();
            bld.WriteLine("using System;");
            bld.WriteLine("using System.Reflection;");
            bld.WriteLine("using System.Runtime.CompilerServices;");
            bld.WriteLine("using System.Runtime.InteropServices;");
            bld.WriteLine("using System.Runtime.Versioning;");
            bld.WriteLine();
            bld.AddAttribute<AssemblyCompanyAttribute>(ass.GetCompany());
            bld.AddAttribute<AssemblyConfigurationAttribute>(ass.GetConfig());
            bld.AddAttribute<AssemblyCopyrightAttribute>(ass.GetCopyright());
            bld.AddAttribute<AssemblyDescriptionAttribute>(ass.GetDesc());
            bld.AddAttribute<AssemblyFileVersionAttribute>(ass.GetFileVersion());
            bld.AddAttribute<AssemblyProductAttribute>(ass.GetProduct());
            bld.AddAttribute<AssemblyTitleAttribute>(ass.GetTitle());
            bld.AddAttribute<AssemblyTrademarkAttribute>(ass.GetTrademark());
            bld.AddAttribute<CompilationRelaxationsAttribute>((int) ass.GetRelaxations());
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(ass.ShouldWrapNonExceptions())
            );
            bld.AddAttribute<AssemblyVersionAttribute>(ass.GetVersion());
            bld.AddAttribute<ComVisibleAttribute>(ass.Manifest.ComVisible);
            bld.AddAttribute<GuidAttribute>(ass.GetGuid());
            bld.AddAttribute<TargetFrameworkAttribute>(ass.GetFrameworkLabel(),
                nameof(TargetFrameworkAttribute.FrameworkDisplayName).Sets(ass.GetFrameworkName())
            );
            return bld.ToString();
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

        private static void AddMembers(NA.IType typ, IHasMembers holder)
        {
            foreach (var member in holder.Members.OfType<MethodDef>())
                AddMethod(typ, member);
            foreach (var member in holder.Members.OfType<EventDef>())
                AddEvent(typ, member);
            foreach (var member in holder.Members.OfType<PropertyDef>())
                AddProperty(typ, member);
            foreach (var member in holder.Members.OfType<IndexerDef>())
                AddIndexer(typ, member);
            foreach (var member in holder.Members.OfType<ConstantDef>())
                AddConstant(typ, member);
        }

        private static void AddEvent(NA.IType typ, EventDef member)
        {
            throw new NotImplementedException();
        }

        private static void AddMethod(NA.IType typ, MethodDef member)
        {
            throw new NotImplementedException();
        }

        private static void EmitClass(NA.INamespace nsp, TypeDef typ)
        {
            var attr = NA.Visibility.Public;
            var cla = N.Create<NA.IClass>(typ.Name, nsp).With(attr);
            AddMembers(cla, typ);
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