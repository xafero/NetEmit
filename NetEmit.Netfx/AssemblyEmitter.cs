using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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

        public string Emit(IAssembly ass)
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

        private static void Emit(IAssembly ass, AssemblyBuilder bld)
        {
            bld.AddAttribute<CompilationRelaxationsAttribute>(8);
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(true)
            );
            var path = Path.GetFileName(ass.GetFileName());
            var mod = bld.DefineDynamicModule(path ?? ass.GetFileName());
            foreach (var nsp in ass.Namespaces)
                Emit(nsp, mod);
        }

        private static void Emit(INamespace nsp, ModuleBuilder mod)
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

        private static string GetFqn(INamespace nsp, IType typ)
            => string.Join(".", nsp.Name, typ.Name);

        private static void EmitClass(INamespace nsp, IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
            var cla = mod.DefineType(GetFqn(nsp, typ), attr);
            cla.AddConstructor();
            cla.CreateType();
        }

        private static void EmitInterface(INamespace nsp, IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Interface
                                        | TypeAttributes.Abstract;
            var intf = mod.DefineType(GetFqn(nsp, typ), attr);
            intf.CreateType();
        }

        private static void EmitDelegate(INamespace nsp, IType typ, ModuleBuilder mod)
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

        private static void EmitEnum(INamespace nsp, IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public;
            var under = typeof(int);
            var enm = mod.DefineEnum(GetFqn(nsp, typ), attr, under);
            enm.CreateType();
        }

        private static void EmitStruct(INamespace nsp, IType typ, ModuleBuilder mod)
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