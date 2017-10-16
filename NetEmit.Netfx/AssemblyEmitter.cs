using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NetEmit.API;

namespace NetEmit.Netfx
{
    public class AssemblyEmitter : IAssemblyEmitter
    {
        public AppDomain Domain { get; }

        public AssemblyEmitter()
        {
            Domain = AppDomain.CurrentDomain;
        }

        public string Emit(AssemblyDef ass)
        {
            var assName = new AssemblyName(ass.Name)
            {
                Version = Version.Parse(ass.GetVersion())
            };
            const AssemblyBuilderAccess assAccess = AssemblyBuilderAccess.Save;
            var file = Path.GetFullPath(ass.GetFileName());
            var dir = Path.GetDirectoryName(file);
            var dyn = Domain.DefineDynamicAssembly(assName, assAccess, dir);
            Emit(ass, dyn);
            dyn.Save(Path.GetFileName(file));
            return file;
        }

        private static void Emit(AssemblyDef ass, AssemblyBuilder bld)
        {
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
            bld.AddAttribute<ComVisibleAttribute>(ass.Manifest.ComVisible);
            bld.AddAttribute<GuidAttribute>(ass.GetGuid());
            bld.AddAttribute<TargetFrameworkAttribute>(ass.GetFrameworkLabel(),
                nameof(TargetFrameworkAttribute.FrameworkDisplayName).Sets(ass.GetFrameworkName())
            );
            var path = Path.GetFileName(ass.GetFileName());
            var mod = bld.DefineDynamicModule(path ?? ass.GetFileName());
            foreach (var nsp in ass.GetNamespaces())
                Emit(nsp, mod);
            if (ass.IsExe())
                ;  //mod.SetUserEntryPoint(null); // TODO: Find method after all!
        }

        private static void Emit(NamespaceDef nsp, ModuleBuilder mod)
        {
            foreach (var typ in nsp.Types)
                switch (typ.Kind)
                {
                    case TypeKind.Enum:
                        EmitEnum(nsp, typ, mod);
                        break;
                    case TypeKind.Struct:
                        EmitStruct(nsp, typ, mod);
                        break;
                    case TypeKind.Delegate:
                        EmitDelegate(nsp, typ, mod);
                        break;
                    case TypeKind.Interface:
                        EmitInterface(nsp, typ, mod);
                        break;
                    case TypeKind.Class:
                        EmitClass(nsp, typ, mod);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(typ.Kind.ToString());
                }
        }

        private static string GetFqn(NamespaceDef nsp, TypeDef typ)
            => string.Join(".", nsp.Name, typ.Name);

        private static void EmitClass(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
            var cla = mod.DefineType(GetFqn(nsp, typ), attr);
            cla.AddConstructor();
            cla.CreateType();
        }

        private static void EmitInterface(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Interface
                                        | TypeAttributes.Abstract;
            var intf = mod.DefineType(GetFqn(nsp, typ), attr);
            intf.CreateType();
        }

        private static void EmitDelegate(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Sealed;
            var under = typeof(MulticastDelegate);
            var dlgt = mod.DefineType(GetFqn(nsp, typ), attr, under);
            const CallingConventions conv = CallingConventions.Standard;
            var tparm = new[] { typeof(object), typeof(IntPtr) };
            const MethodAttributes cattr = MethodAttributes.Public | MethodAttributes.HideBySig;
            var cstr = dlgt.DefineConstructor(cattr, conv, tparm);
            cstr.DefineParameter(1, ParameterAttributes.None, "object");
            cstr.DefineParameter(2, ParameterAttributes.None, "method");
            cstr.SetImplementationFlags(MethodImplAttributes.Runtime);
            const MethodAttributes mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                                           MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var inv = dlgt.DefineMethod("Invoke", mattr);
            inv.SetImplementationFlags(MethodImplAttributes.Runtime);
            var bgi = dlgt.DefineMethod("BeginInvoke", mattr, typeof(IAsyncResult),
                new[] { typeof(AsyncCallback), typeof(object) });
            bgi.DefineParameter(1, ParameterAttributes.None, "callback");
            bgi.DefineParameter(2, ParameterAttributes.None, "object");
            bgi.SetImplementationFlags(MethodImplAttributes.Runtime);
            var ebi = dlgt.DefineMethod("EndInvoke", mattr, typeof(void), new[] { typeof(IAsyncResult) });
            ebi.DefineParameter(1, ParameterAttributes.None, "result");
            ebi.SetImplementationFlags(MethodImplAttributes.Runtime);
            dlgt.CreateType();
        }

        private static void EmitEnum(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public;
            var under = typeof(int);
            var enm = mod.DefineEnum(GetFqn(nsp, typ), attr, under);
            enm.FixUnderlyingVisibility();
            enm.CreateType();
        }

        private static void EmitStruct(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public
                                        | TypeAttributes.SequentialLayout
                                        | TypeAttributes.Sealed
                                        | TypeAttributes.BeforeFieldInit;
            var under = typeof(ValueType);
            var stru = mod.DefineType(GetFqn(nsp, typ), attr, under);
            stru.CreateType();
        }

        public void Dispose()
        {
            if (AppDomain.CurrentDomain == Domain)
                return;
            AppDomain.Unload(Domain);
        }

        public override string ToString() => Domain.ToString();
    }
}