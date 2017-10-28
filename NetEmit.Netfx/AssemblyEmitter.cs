using System;
using System.IO;
using System.Linq;
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
            bld.AddAttribute<AssemblyProductAttribute>(ass.GetProduct());
            bld.AddAttribute<AssemblyCompanyAttribute>(ass.GetCompany());
            bld.AddAttribute<AssemblyConfigurationAttribute>(ass.GetConfig());
            bld.AddAttribute<AssemblyCopyrightAttribute>(ass.GetCopyright());
            bld.AddAttribute<AssemblyDescriptionAttribute>(ass.GetDesc());
            bld.AddAttribute<AssemblyFileVersionAttribute>(ass.GetFileVersion());
            bld.AddAttribute<CompilationRelaxationsAttribute>((int)ass.GetRelaxations());
            bld.AddAttribute<AssemblyTitleAttribute>(ass.GetTitle());
            bld.AddAttribute<AssemblyTrademarkAttribute>(ass.GetTrademark());
            bld.AddAttribute<ComVisibleAttribute>(ass.Manifest.ComVisible);
            bld.AddAttribute<TargetFrameworkAttribute>(ass.GetFrameworkLabel(),
                nameof(TargetFrameworkAttribute.FrameworkDisplayName).Sets(ass.GetFrameworkName())
            );
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(ass.ShouldWrapNonExceptions())
            );
            bld.AddAttribute<GuidAttribute>(ass.GetGuid());
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
            AddMembers(mod, cla, typ);
            cla.CreateType();
        }

        private static void EmitInterface(NamespaceDef nsp, TypeDef typ, ModuleBuilder mod)
        {
            const TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Interface
                                        | TypeAttributes.Abstract;
            var intf = mod.DefineType(GetFqn(nsp, typ), attr);
            AddMembers(mod, intf, typ);
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
            var index = 0;
            foreach (var member in typ.Members.OfType<ConstantDef>())
                enm.DefineLiteral(member.Name, index++);
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
            AddMembers(mod, stru, typ);
            stru.CreateType();
        }

        private static void AddMembers(ModuleBuilder mod, TypeBuilder typ, IHasMembers holder)
        {
            foreach (var member in holder.Members.OfType<MethodDef>())
                AddMethod(mod, typ, member);
            foreach (var member in holder.Members.OfType<PropertyDef>())
                AddProperty(mod, typ, member);
            foreach (var member in holder.Members.OfType<EventDef>())
                AddEvent(mod, typ, member);
            foreach (var member in holder.Members.OfType<IndexerDef>())
                AddIndexer(mod, typ, member);
            foreach (var member in holder.Members.OfType<ConstantDef>())
                AddConstant(mod, typ, member);
        }

        private static void AddConstant(ModuleBuilder mod, TypeBuilder typ, ConstantDef member)
        {
            var objRef = typ.IsEnum ? typ : typeof(object);
            const FieldAttributes attr = FieldAttributes.Public | FieldAttributes.Literal
                                         | FieldAttributes.Static;
            const int constInt = 1;
            var fld = typ.DefineField(member.Name, objRef, attr);
            if (typ.IsEnum)
                fld.SetConstant(constInt);
        }

        private static void CreateProperty(TypeBuilder typ, string name,
            params Tuple<string, Type>[] args)
        {
            var voidRef = typeof(void);
            var prpRef = typeof(string);
            var pargs = args.Select(a => a.Item2).ToArray();
            const PropertyAttributes attr = PropertyAttributes.None;
            var mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName | MethodAttributes.NewSlot;
            if (typ.IsInterface)
                mattr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            const CallingConventions call = CallingConventions.HasThis;
            var prop = typ.DefineProperty(name, attr, call, prpRef, pargs);
            var getter = typ.DefineMethod($"get_{name}", mattr, prpRef, pargs);
            if (!typ.IsInterface)
                getter.SetMethodBody(new byte[1], 0, new byte[0], null, null);
            getter.ApplyParams(args);
            var setPrms = pargs.Concat(new[] { prpRef }).ToArray();
            var setter = typ.DefineMethod($"set_{name}", mattr, voidRef, setPrms);
            setter.DefineParameter(setPrms.Length, ParameterAttributes.None, "value");
            if (!typ.IsInterface)
                setter.SetMethodBody(new byte[1], 0, new byte[0], null, null);
            setter.ApplyParams(args);
            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);
        }

        private static void AddIndexer(ModuleBuilder mod, TypeBuilder typ, IndexerDef member)
        {
            var intr = typeof(int);
            CreateProperty(typ, member.Name, Tuple.Create("index", intr));
            typ.AddAttribute<DefaultMemberAttribute>("Item");
        }

        private static void AddProperty(ModuleBuilder mod, TypeBuilder typ, PropertyDef member)
        {
            CreateProperty(typ, member.Name);
        }

        private static void AddEvent(ModuleBuilder mod, TypeBuilder typ, EventDef member)
        {
            var voidRef = typeof(void);
            var evth = typeof(EventHandler);
            const EventAttributes attr = EventAttributes.None;
            var mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                                           MethodAttributes.NewSlot | MethodAttributes.SpecialName |
                                           MethodAttributes.Virtual;
            if (typ.IsInterface)
                mattr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            var evt = typ.DefineEvent(member.Name, attr, evth);
            var adder = typ.DefineMethod($"add_{member.Name}", mattr, voidRef, new[] { evth });
            adder.DefineParameter(1, ParameterAttributes.None, "value");
            if (!typ.IsInterface)
                adder.SetMethodBody(new byte[1], 0, new byte[0], null, null);
            var remover = typ.DefineMethod($"remove_{member.Name}", mattr, voidRef, new[] { evth });
            remover.DefineParameter(1, ParameterAttributes.None, "value");
            if (!typ.IsInterface)
                remover.SetMethodBody(new byte[1], 0, new byte[0], null, null);
            evt.SetAddOnMethod(adder);
            evt.SetRemoveOnMethod(remover);
        }

        private static void AddMethod(ModuleBuilder mod, TypeBuilder typ, MethodDef member)
        {
            var retType = typeof(void);
            var prmTypes = new Type[0];
            var attr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
            if (typ.IsInterface)
                attr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            var meth = typ.DefineMethod(member.Name, attr, retType, prmTypes);
            if (!typ.IsInterface)
                meth.SetMethodBody(new byte[1], 0, new byte[0], null, null);
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