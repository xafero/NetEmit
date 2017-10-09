using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetEmit.API;
using NetEmit.Core;

namespace NetEmit.Netfx
{
    public class AssemblyEmitter : IAssemblyEmitter
    {
        public AppDomain Domain { get; }

        public AssemblyEmitter()
        {
            Domain = AppDomain.CurrentDomain;
        }

        public void Emit(IAssembly ass)
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

            Console.WriteLine(file);
        }

        private static void Emit(IAssembly ass, AssemblyBuilder bld)
        {
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(true)
            );
            var mod = bld.DefineDynamicModule(ass.GetFileName());
            foreach (var nsp in ass.Namespaces)
                Emit(nsp, mod);
        }

        private static void Emit(INamespace nsp, ModuleBuilder mod)
        {
            foreach (var typ in nsp.Types)
                switch (typ.Kind)
                {
                    case TypeKind.Enum:
                        EmitEnum(typ, mod);
                        break;
                    case TypeKind.Struct:
                        EmitStruct(typ, mod);
                        break;
                    case TypeKind.Delegate:
                        EmitDelegate(typ, mod);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(typ.Kind.ToString());
                }
        }

        private static void EmitDelegate(IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public;
            var under = typeof(MulticastDelegate);
            var dlgt = mod.DefineType(typ.Name, attr, under);
            const MethodAttributes mattr = MethodAttributes.Public;
            const CallingConventions conv = CallingConventions.Standard;
            var tparm = new[] {typeof(object), typeof(IntPtr)};
            var cstr = dlgt.DefineConstructor(mattr, conv, tparm);
            cstr.SetImplementationFlags(MethodImplAttributes.Runtime);
            dlgt.CreateType();
        }

        private static void EmitEnum(IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public;
            var under = typeof(byte);
            var enm = mod.DefineEnum(typ.Name, attr, under);
            enm.CreateType();
        }

        private static void EmitStruct(IType typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public;
            var under = typeof(ValueType);
            var stru = mod.DefineType(typ.Name, attr, under);
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