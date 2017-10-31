using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using NetEmit.API;

using MA = System.Reflection.MethodAttributes;

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
            AddMembers(mod, cla, typ);
            cla.AddConstructor();
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
            const MA cattr = MA.Public | MA.HideBySig;
            var cstr = dlgt.DefineConstructor(cattr, conv, tparm);
            cstr.DefineParameter(1, ParameterAttributes.None, "object");
            cstr.DefineParameter(2, ParameterAttributes.None, "method");
            cstr.SetImplementationFlags(MethodImplAttributes.Runtime);
            const MA mattr = MA.Public | MA.HideBySig | MA.NewSlot | MA.Virtual;
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
            foreach (var member in holder.Members.OfType<ConstantDef>())
                AddConstant(mod, typ, member);
            foreach (var member in holder.Members.OfType<PropertyDef>())
                AddProperty(mod, typ, member);
            foreach (var member in holder.Members.OfType<EventDef>())
                AddEvent(mod, typ, member);
            foreach (var member in holder.Members.OfType<IndexerDef>())
                AddIndexer(mod, typ, member);
        }

        private static void AddConstant(ModuleBuilder mod, TypeBuilder typ, ConstantDef member)
        {
            var objRef = typ.IsEnum ? typ : typeof(object);
            var attr = FieldAttributes.Public;
            if (typ.IsEnum)
                attr |= FieldAttributes.Literal | FieldAttributes.Static;
            const int constInt = 1;
            var fld = typ.DefineField(member.Name, objRef, attr);
            if (typ.IsEnum)
                fld.SetConstant(constInt);
        }

        private static PropertyBuilder CreateProperty(TypeBuilder typ, string name,
            params Tuple<string, Type>[] args)
        {
            var voidRef = typeof(void);
            var prpRef = typeof(string);
            var pargs = args.Select(a => a.Item2).ToArray();
            const PropertyAttributes attr = PropertyAttributes.None;
            var mattr = MA.Public | MA.HideBySig | MA.SpecialName;
            if (typ.IsInterface)
                mattr |= MA.Abstract | MA.Virtual | MA.NewSlot;
            const CallingConventions call = CallingConventions.HasThis;
            var prop = typ.DefineProperty(name, attr, call, prpRef, pargs);
            var getter = typ.DefineMethod($"get_{name}", mattr, prpRef, pargs);
            getter.ApplyParams(args);
            var setPrms = pargs.Concat(new[] { prpRef }).ToArray();
            var setter = typ.DefineMethod($"set_{name}", mattr, voidRef, setPrms);
            setter.DefineParameter(setPrms.Length, ParameterAttributes.None, "value");
            setter.ApplyParams(args);
            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);
            return prop;
        }

        private static void AddIndexer(ModuleBuilder mod, TypeBuilder typ, IndexerDef member)
        {
            var intr = typeof(int);
            var prop = CreateProperty(typ, member.Name, Tuple.Create("index", intr));
            typ.AddAttribute<DefaultMemberAttribute>("Item");
            if (typ.IsAbstract())
                return;
            AddDefaultIndexerImpl((MethodBuilder)prop.GetMethod, (MethodBuilder)prop.SetMethod);
        }

        private static void AddProperty(ModuleBuilder mod, TypeBuilder typ, PropertyDef member)
        {
            var prop = CreateProperty(typ, member.Name);
            if (typ.IsAbstract())
                return;
            AddDefaultPropertyImpl((MethodBuilder)prop.GetMethod, (MethodBuilder)prop.SetMethod);
        }

        private static void AddDefaultPropertyImpl(MethodBuilder get, MethodBuilder set)
        {
            var backing = AddPropertyBackingField(get);
            backing.AddAttribute<CompilerGeneratedAttribute>();
            get.AddAttribute<CompilerGeneratedAttribute>();
            AddMethodBody(get, i =>
            {
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldfld, backing);
            });
            set.AddAttribute<CompilerGeneratedAttribute>();
            AddMethodBody(set, i =>
            {
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldarg_1);
                i.Emit(OpCodes.Stfld, backing);
            });
        }

        private static void AddDefaultIndexerImpl(MethodBuilder get, MethodBuilder set)
        {
            var dictType = typeof(Dictionary<int, string>);
            var getItem = dictType.GetMethods().First(m => m.Name == "get_Item");
            var setItem = dictType.GetMethods().First(m => m.Name == "set_Item");
            var backing = AddPropertyBackingField(get, dictType, "idx");
            AddMethodBody(get, i =>
            {
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldfld, backing);
                i.Emit(OpCodes.Ldarg_1);
                i.Emit(OpCodes.Callvirt, getItem);
            });
            AddMethodBody(set, i =>
            {
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldfld, backing);
                i.Emit(OpCodes.Ldarg_1);
                i.Emit(OpCodes.Ldarg_2);
                i.Emit(OpCodes.Callvirt, setItem);
            });
        }

        private static FieldBuilder AddPropertyBackingField(MethodBuilder get,
            Type rt = null, string fieldName = null)
        {
            var typ = (TypeBuilder)get.DeclaringType;
            var name = get.Name.Split(new[] { '_' }, 2).Last();
            const FieldAttributes attr = FieldAttributes.Private;
            fieldName = fieldName ?? $"<{name}>k__BackingField";
            return typ?.DefineField(fieldName, rt ?? get.ReturnType, attr);
        }

        private static FieldBuilder AddEventBackingField(MethodBuilder add, Type prmType)
        {
            var typ = (TypeBuilder)add.DeclaringType;
            var name = add.Name.Split(new[] { '_' }, 2).Last();
            const FieldAttributes attr = FieldAttributes.Private;
            return typ?.DefineField($"{name}", prmType, attr);
        }

        private static void AddEvent(ModuleBuilder mod, TypeBuilder typ, EventDef member)
        {
            var voidRef = typeof(void);
            var evth = typeof(EventHandler);
            const EventAttributes attr = EventAttributes.None;
            var mattr = MA.Public | MA.HideBySig | MA.SpecialName;
            if (typ.IsAbstract())
                mattr |= MA.Abstract | MA.Virtual | MA.NewSlot;
            var evt = typ.DefineEvent(member.Name, attr, evth);
            var adder = typ.DefineMethod($"add_{member.Name}", mattr, voidRef, new[] { evth });
            adder.DefineParameter(1, ParameterAttributes.None, "value");
            var remover = typ.DefineMethod($"remove_{member.Name}", mattr, voidRef, new[] { evth });
            remover.DefineParameter(1, ParameterAttributes.None, "value");
            evt.SetAddOnMethod(adder);
            evt.SetRemoveOnMethod(remover);
            if (typ.IsAbstract())
                return;
            AddDefaultEventImpl(adder, remover, evth);
        }

        private static void AddDefaultEventImpl(MethodBuilder add, MethodBuilder rem, Type evtType)
        {
            var dlgt = typeof(Delegate);
            var combineMeth = dlgt.GetMethod(nameof(Delegate.Combine), new[] { dlgt, dlgt });
            var removeMeth = dlgt.GetMethod(nameof(Delegate.Remove), new[] { dlgt, dlgt });
            var compareMeth = typeof(Interlocked).GetMethods().Where(
                    m => m.Name == nameof(Interlocked.CompareExchange)).Single(
                    m => m.GetParameters().Length == 3 && m.IsGenericMethod)
                .MakeGenericMethod(typeof(EventHandler));
            var backing = AddEventBackingField(add, evtType);
            var evtt = typeof(EventHandler);
            Action<ILGenerator> init = il =>
            {
                il.DeclareLocal(evtt);
                il.DeclareLocal(evtt);
                il.DeclareLocal(evtt);
            };
            AddMethodBody(add, i =>
            {
                init(i);
                var jmp = i.DefineLabel();
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldfld, backing);
                i.Emit(OpCodes.Stloc_0);
                i.MarkLabel(jmp);
                i.Emit(OpCodes.Ldloc_0);
                i.Emit(OpCodes.Stloc_1);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Ldarg_1);
                i.Emit(OpCodes.Call, combineMeth);
                i.Emit(OpCodes.Castclass, backing.FieldType);
                i.Emit(OpCodes.Stloc_2);
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldflda, backing);
                i.Emit(OpCodes.Ldloc_2);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Call, compareMeth);
                i.Emit(OpCodes.Stloc_0);
                i.Emit(OpCodes.Ldloc_0);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Bne_Un_S, jmp);
            });
            AddMethodBody(rem, i =>
            {
                init(i);
                var jmp = i.DefineLabel();
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldfld, backing);
                i.Emit(OpCodes.Stloc_0);
                i.MarkLabel(jmp);
                i.Emit(OpCodes.Ldloc_0);
                i.Emit(OpCodes.Stloc_1);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Ldarg_1);
                i.Emit(OpCodes.Call, removeMeth);
                i.Emit(OpCodes.Castclass, backing.FieldType);
                i.Emit(OpCodes.Stloc_2);
                i.Emit(OpCodes.Ldarg_0);
                i.Emit(OpCodes.Ldflda, backing);
                i.Emit(OpCodes.Ldloc_2);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Call, compareMeth);
                i.Emit(OpCodes.Stloc_0);
                i.Emit(OpCodes.Ldloc_0);
                i.Emit(OpCodes.Ldloc_1);
                i.Emit(OpCodes.Bne_Un_S, jmp);
            });
        }

        private static void AddMethod(ModuleBuilder mod, TypeBuilder typ, MethodDef member)
        {
            var retType = typeof(void);
            var prmTypes = new Type[0];
            var attr = MA.Public | MA.HideBySig;
            if (typ.IsInterface)
                attr |= MA.Abstract | MA.Virtual | MA.NewSlot;
            var meth = typ.DefineMethod(member.Name, attr, retType, prmTypes);
            if (!typ.IsInterface)
                AddMethodBody(meth);
        }

        private static void AddMethodBody(MethodBuilder meth, Action<ILGenerator> writeIl = null)
        {
            var ils = meth.GetILGenerator();
            writeIl?.Invoke(ils);
            ils.Emit(OpCodes.Ret);
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