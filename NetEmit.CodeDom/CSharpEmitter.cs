using System;
using System.CodeDom;
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
using Noaster.Impl.Parts;
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
            bld.AddAttribute<CompilationRelaxationsAttribute>((int)ass.GetRelaxations());
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
            foreach (var member in holder.Members.OfType<ConstantDef>())
                AddConstant(typ, member);
            foreach (var member in holder.Members.OfType<PropertyDef>())
                AddProperty(typ, member);
            foreach (var member in holder.Members.OfType<EventDef>())
                AddEvent(typ, member);
            foreach (var member in holder.Members.OfType<IndexerDef>())
                AddIndexer(typ, member);
        }

        private static void AddConstant(NA.IType typ, ConstantDef member)
        {
            var evaler = typ as NA.IEnum;
            if (evaler != null)
            {
                var val = N.Create<NA.IEnumVal>(member.Name);
                evaler.Values.Add(val);
            }
            var holder = typ as NA.IHasFields;
            if (holder == null)
                return;
            const NA.Visibility attr = NA.Visibility.Public;
            var fld = N.Create<NA.IField>(member.Name).With(attr);
            holder.Fields.Add(fld);
        }

        private static void AddIndexer(NA.IType typ, IndexerDef member)
        {
            var holder = typ as NA.IHasIndexers;
            if (holder == null)
                return;
            const NA.Visibility attr = NA.Visibility.Public;
            var indx = N.Create<NA.IIndexer>(member.Name).With(attr);
            indx.Type = typeof(string).FullName;
            var parm = N.Create<NA.IParameter>("index");
            parm.Type = typeof(int).FullName;
            indx.Parameters.Add(parm);
            holder.Indexers.Add(indx);
            var fielder = typ as NA.IHasFields;
            if (typ.IsAbstract() || fielder == null)
                return;
            var dictType = $"System.Collections.Generic.Dictionary<{parm.Type},{indx.Type}>";
            var idxBack = new FieldImpl("idx", dictType);
            fielder.Fields.Add(idxBack);
            indx.Getter = "return this.idx[index];";
            indx.Setter = "this.idx[index] = value;";
        }

        private static void AddProperty(NA.IType typ, PropertyDef member)
        {
            var holder = typ as NA.IHasProperties;
            if (holder == null)
                return;
            const NA.Visibility attr = NA.Visibility.Public;
            var prop = N.Create<NA.IProperty>(member.Name).With(attr);
            prop.Type = typeof(string).FullName;
            holder.Properties.Add(prop);
        }

        private static void AddEvent(NA.IType typ, EventDef member)
        {
            var holder = typ as NA.IHasEvents;
            if (holder == null)
                return;
            const NA.Visibility attr = NA.Visibility.Public;
            var evt = N.Create<NA.IEvent>(member.Name).With(attr);
            evt.Type = typeof(EventHandler).FullName;
            holder.Events.Add(evt);
        }

        private static void AddMethod(NA.IType typ, MethodDef member)
        {
            var holder = typ as NA.IHasMethods;
            if (holder == null)
                return;
            const NA.Visibility attr = NA.Visibility.Public;
            var meth = N.Create<NA.IMethod>(member.Name).With(attr);
            holder.Methods.Add(meth);
        }

        private static void EmitClass(NA.INamespace nsp, TypeDef typ)
        {
            const NA.Visibility attr = NA.Visibility.Public;
            var cla = N.Create<NA.IClass>(typ.Name, nsp).With(attr);
            AddMembers(cla, typ);
        }

        private static void EmitInterface(NA.INamespace nsp, TypeDef typ)
        {
            const NA.Visibility attr = NA.Visibility.Public;
            var i = N.Create<NA.IInterface>(typ.Name, nsp).With(attr);
            AddMembers(i, typ);
        }

        private static void EmitDelegate(NA.INamespace nsp, TypeDef typ)
        {
            const NA.Visibility attr = NA.Visibility.Public;
            var d = N.Create<NA.IDelegate>(typ.Name, nsp).With(attr);
            AddMembers(d, typ);
        }

        private static void EmitStruct(NA.INamespace nsp, TypeDef typ)
        {
            const NA.Visibility attr = NA.Visibility.Public;
            var s = N.Create<NA.IStruct>(typ.Name, nsp).With(attr);
            AddMembers(s, typ);
        }

        private static void EmitEnum(NA.INamespace nsp, TypeDef typ)
        {
            const NA.Visibility attr = NA.Visibility.Public;
            var e = N.Create<NA.IEnum>(typ.Name, nsp).With(attr);
            AddMembers(e, typ);
        }

        private static string ToText(CompilerResults results)
            => string.Join(Environment.NewLine, results.Errors.OfType<CompilerError>());

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}