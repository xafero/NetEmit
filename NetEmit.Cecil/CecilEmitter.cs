using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetEmit.API;
using EventAttributes = Mono.Cecil.EventAttributes;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MA = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace NetEmit.Cecil
{
    public class CecilEmitter : IAssemblyEmitter
    {
        public string Emit(AssemblyDef ass)
        {
            var ver = Version.Parse(ass.GetVersion());
            var assName = new AssemblyNameDefinition(ass.Name, ver)
            {
                HashAlgorithm = AssemblyHashAlgorithm.SHA1
            };
            var moduleName = Path.GetFileName(ass.GetFileName());
            var file = Path.GetFullPath(ass.GetFileName());
            using (var resolver = new DefaultAssemblyResolver())
            {
                var dir = Path.GetDirectoryName(file);
                resolver.AddSearchDirectory(dir);
                var parms = new ModuleParameters
                {
                    Kind = GetKind(ass),
                    Runtime = TargetRuntime.Net_4_0,
                    AssemblyResolver = resolver
                };
                ApplyArch(ass, parms);
                using (var dyn = AssemblyDefinition.CreateAssembly(assName, moduleName, parms))
                {
                    Emit(ass, dyn);
                    var wparms = new WriterParameters { WriteSymbols = false };
                    dyn.Write(file, wparms);
                }
            }
            return file;
        }

        private static void ApplyArch(AssemblyDef ass, ModuleParameters parms)
        {
            TargetArchitecture arch;
            if (Enum.TryParse(ass.GetArchitecture(), true, out arch))
                parms.Architecture = arch;
        }

        private static void ApplyAttributes(ModuleDefinition mod, AssemblyDef ass)
        {
            ModuleAttributes modAttrs;
            if (Enum.TryParse(ass.GetCorFlags(), true, out modAttrs))
                mod.Attributes = modAttrs;
        }

        private static ModuleKind GetKind(AssemblyDef ass)
            => ass.IsExe() ? (ass.HasGui() ? ModuleKind.Windows : ModuleKind.Console) : ModuleKind.Dll;

        private static void EmitResources(ModuleDefinition mod, IEnumerable<ResourceDef> resources)
        {
            foreach (var resource in resources)
            {
                var name = resource.Name;
                const ManifestResourceAttributes attr = ManifestResourceAttributes.Public;
                var data = new byte[resource.Length ?? 0];
                var embRes = new EmbeddedResource(name, attr, data);
                mod.Resources.Add(embRes);
            }
        }

        private static void Emit(AssemblyDef ass, AssemblyDefinition bld)
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
            var mod = bld.MainModule;
            EmitResources(mod, ass.Resources);
            ApplyAttributes(mod, ass);
            foreach (var nsp in ass.GetNamespaces())
                Emit(nsp, mod);
            if (ass.IsExe())
                mod.EntryPoint = null; // TODO: Find method after all!
        }

        private static void Emit(NamespaceDef nsp, ModuleDefinition mod)
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

        private static void EmitEnum(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var enmRef = mod.ImportReference(typeof(Enum));
            var enm = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public | TypeAttributes.Sealed, enmRef);
            var underRef = mod.ImportReference(typeof(int));
            enm.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.RTSpecialName
                                                          | FieldAttributes.SpecialName, underRef));
            mod.Types.Add(enm);
            AddMembers(mod, enm, typ);
        }

        private static void EmitStruct(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var valRef = mod.ImportReference(typeof(ValueType));
            var stru = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public
                                                              | TypeAttributes.SequentialLayout | TypeAttributes.Sealed
                                                              | TypeAttributes.BeforeFieldInit, valRef);
            mod.Types.Add(stru);
            AddMembers(mod, stru, typ);
        }

        private static void EmitDelegate(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var dlgRef = mod.ImportReference(typeof(MulticastDelegate));
            var dlg = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public | TypeAttributes.Sealed, dlgRef);
            dlg.AddConstructor(mod, null, Tuple.Create("object", typeof(object)),
                Tuple.Create("method", typeof(IntPtr)));
            const MA mattr = MA.Public | MA.HideBySig | MA.NewSlot | MA.Virtual;
            var invMeth = new MethodDefinition("Invoke", mattr, voidRef) { IsRuntime = true };
            dlg.Methods.Add(invMeth);
            var arr = mod.ImportReference(typeof(IAsyncResult));
            var begMeth = new MethodDefinition("BeginInvoke", mattr, arr) { IsRuntime = true };
            var parm = new ParameterDefinition(mod.ImportReference(typeof(AsyncCallback))) { Name = "callback" };
            begMeth.Parameters.Add(parm);
            parm = new ParameterDefinition(mod.ImportReference(typeof(object))) { Name = "object" };
            begMeth.Parameters.Add(parm);
            dlg.Methods.Add(begMeth);
            var endMeth = new MethodDefinition("EndInvoke", mattr, voidRef) { IsRuntime = true };
            parm = new ParameterDefinition(mod.ImportReference(typeof(IAsyncResult))) { Name = "result" };
            endMeth.Parameters.Add(parm);
            dlg.Methods.Add(endMeth);
            mod.Types.Add(dlg);
        }

        private static void EmitInterface(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var intf = new TypeDefinition(nsp.Name, typ.Name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            mod.Types.Add(intf);
            AddMembers(mod, intf, typ);
        }

        private static void EmitClass(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var baseRef = mod.ImportReference(GetBaseType<object>(typ));
            var cla = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public
                                                             | TypeAttributes.BeforeFieldInit, baseRef);
            mod.Types.Add(cla);
            AddMembers(mod, cla, typ);
            cla.AddConstructor(mod, 1);
        }

        private static void AddMembers(ModuleDefinition mod, TypeDefinition typ, IHasMembers holder)
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

        private static void AddConstant(ModuleDefinition mod, TypeDefinition typ, ConstantDef member)
        {
            object constObj = null;
            var objRef = typ.IsEnum ? typ : mod.ImportReference(typeof(object));
            var attr = FieldAttributes.Public;
            if (typ.IsEnum)
            {
                attr |= FieldAttributes.Static | FieldAttributes.Literal;
                constObj = typ.Fields.Count - 1;
            }
            if (constObj != null)
                attr |= FieldAttributes.HasDefault;
            var fld = new FieldDefinition(member.Name, attr, objRef);
            typ.Fields.Add(fld);
            if (constObj != null)
                fld.Constant = constObj;
        }

        private static void AddMethod(ModuleDefinition mod, TypeDefinition typ, MethodDef member)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var attr = MA.Public | MA.HideBySig;
            if (typ.IsAbstract())
                attr |= MA.Abstract | MA.Virtual | MA.NewSlot;
            var meth = new MethodDefinition(member.Name, attr, voidRef);
            if (!meth.IsAbstract)
                AddMethodBody(meth);
            typ.Methods.Add(meth);
        }

        private static void AddMethodBody(MethodDefinition meth, Action<ILProcessor> writeIl = null)
        {
            var body = meth.Body ?? new MethodBody(meth);
            var ils = body.GetILProcessor();
            writeIl?.Invoke(ils);
            ils.Append(ils.Create(OpCodes.Ret));
            meth.Body = body;
        }

        private static PropertyDefinition CreateProperty(ModuleDefinition mod, string name, bool isAbstract)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var prpRef = mod.ImportReference(typeof(string));
            const PropertyAttributes pattr = PropertyAttributes.None;
            var mattr = MA.Public | MA.HideBySig | MA.SpecialName;
            if (isAbstract)
                mattr |= MA.Abstract | MA.Virtual | MA.NewSlot;
            var valParm = new ParameterDefinition("value", ParameterAttributes.None, prpRef);
            return new PropertyDefinition(name, pattr, prpRef)
            {
                GetMethod = new MethodDefinition($"get_{name}", mattr, prpRef),
                SetMethod = new MethodDefinition($"set_{name}", mattr, voidRef)
                { Parameters = { valParm } }
            };
        }

        private static void AddProperty(ModuleDefinition mod, TypeDefinition typ, PropertyDef member)
        {
            var prop = CreateProperty(mod, member.Name, typ.IsInterface | typ.IsAbstract);
            typ.Methods.Add(prop.GetMethod);
            typ.Methods.Add(prop.SetMethod);
            typ.Properties.Add(prop);
            if (typ.IsAbstract())
                return;
            AddDefaultPropertyImpl(prop.GetMethod, prop.SetMethod);
        }

        private static void AddDefaultPropertyImpl(MethodDefinition get, MethodDefinition set)
        {
            var backing = AddPropertyBackingField(get);
            backing.AddAttribute<CompilerGeneratedAttribute>();
            get.AddAttribute<CompilerGeneratedAttribute>();
            AddMethodBody(get, i =>
            {
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldfld, backing));
            });
            set.AddAttribute<CompilerGeneratedAttribute>();
            AddMethodBody(set, i =>
            {
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldarg_1));
                i.Append(i.Create(OpCodes.Stfld, backing));
            });
        }

        private static FieldDefinition AddPropertyBackingField(MethodDefinition get)
        {
            var typ = get.DeclaringType;
            var name = get.Name.Split(new[] { '_' }, 2).Last();
            const FieldAttributes attr = FieldAttributes.Private;
            var backing = new FieldDefinition($"<{name}>k__BackingField", attr, get.ReturnType);
            typ.Fields.Add(backing);
            return backing;
        }

        private static FieldDefinition AddEventBackingField(MethodDefinition add)
        {
            var typ = add.DeclaringType;
            var name = add.Name.Split(new[] { '_' }, 2).Last();
            const FieldAttributes attr = FieldAttributes.Private;
            var backing = new FieldDefinition($"{name}", attr, add.Parameters.First().ParameterType);
            var firstNot = typ.Fields.FirstOrDefault(f => f.Name.Contains("k__BackingField"));
            var index = firstNot == null ? 0 : typ.Fields.IndexOf(firstNot);
            typ.Fields.Insert(index, backing);
            return backing;
        }

        private static void AddDefaultEventImpl(MethodDefinition add, MethodDefinition rem)
        {
            var mod = add.DeclaringType.Module;
            var backing = AddEventBackingField(add);
            var dlgt = typeof(Delegate);
            var combineMeth = dlgt.GetMethod(nameof(Delegate.Combine), new[] { dlgt, dlgt });
            var removeMeth = dlgt.GetMethod(nameof(Delegate.Remove), new[] { dlgt, dlgt });
            var compareMeth = typeof(Interlocked).GetMethods().Where(
                    m => m.Name == nameof(Interlocked.CompareExchange)).Single(
                    m => m.GetParameters().Length == 3 && m.IsGenericMethod)
                .MakeGenericMethod(typeof(EventHandler));
            var evtt = mod.ImportReference(typeof(EventHandler));
            Action<MethodBody> init = body =>
            {
                body.Variables.Add(new VariableDefinition(evtt));
                body.Variables.Add(new VariableDefinition(evtt));
                body.Variables.Add(new VariableDefinition(evtt));
                body.InitLocals = true;
            };
            init(add.Body);
            AddMethodBody(add, i =>
            {
                Instruction jmp;
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldfld, backing));
                i.Append(i.Create(OpCodes.Stloc_0));
                i.Append(jmp = i.Create(OpCodes.Ldloc_0));
                i.Append(i.Create(OpCodes.Stloc_1));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Ldarg_1));
                i.Append(i.Create(OpCodes.Call, mod.ImportReference(combineMeth)));
                i.Append(i.Create(OpCodes.Castclass, backing.FieldType));
                i.Append(i.Create(OpCodes.Stloc_2));
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldflda, backing));
                i.Append(i.Create(OpCodes.Ldloc_2));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Call, mod.ImportReference(compareMeth)));
                i.Append(i.Create(OpCodes.Stloc_0));
                i.Append(i.Create(OpCodes.Ldloc_0));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Bne_Un_S, jmp));
            });
            init(rem.Body);
            AddMethodBody(rem, i =>
            {
                Instruction jmp;
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldfld, backing));
                i.Append(i.Create(OpCodes.Stloc_0));
                i.Append(jmp = i.Create(OpCodes.Ldloc_0));
                i.Append(i.Create(OpCodes.Stloc_1));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Ldarg_1));
                i.Append(i.Create(OpCodes.Call, mod.ImportReference(removeMeth)));
                i.Append(i.Create(OpCodes.Castclass, backing.FieldType));
                i.Append(i.Create(OpCodes.Stloc_2));
                i.Append(i.Create(OpCodes.Ldarg_0));
                i.Append(i.Create(OpCodes.Ldflda, backing));
                i.Append(i.Create(OpCodes.Ldloc_2));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Call, mod.ImportReference(compareMeth)));
                i.Append(i.Create(OpCodes.Stloc_0));
                i.Append(i.Create(OpCodes.Ldloc_0));
                i.Append(i.Create(OpCodes.Ldloc_1));
                i.Append(i.Create(OpCodes.Bne_Un_S, jmp));
            });
        }

        private static void AddIndexer(ModuleDefinition mod, TypeDefinition typ, IndexerDef member)
        {
            var intr = mod.ImportReference(typeof(int));
            var indx = CreateProperty(mod, member.Name, typ.IsInterface | typ.IsAbstract);
            const ParameterAttributes pattr = ParameterAttributes.None;
            var parm = new ParameterDefinition("index", pattr, intr);
            var getter = indx.GetMethod;
            getter.Parameters.Add(parm);
            typ.Methods.Add(getter);
            var setter = indx.SetMethod;
            setter.Parameters.Insert(0, parm);
            typ.Methods.Add(setter);
            typ.Properties.Add(indx);
            typ.AddAttribute<DefaultMemberAttribute>("Item");
        }

        private static void AddEvent(ModuleDefinition mod, TypeDefinition typ, EventDef member)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var evth = mod.ImportReference(typeof(EventHandler));
            const EventAttributes eattr = EventAttributes.None;
            var mattr = MA.Public | MA.HideBySig | MA.SpecialName;
            if (typ.IsAbstract())
                mattr |= MA.NewSlot | MA.Abstract | MA.Virtual;
            var valParm = new ParameterDefinition("value", ParameterAttributes.None, evth);
            var evt = new EventDefinition(member.Name, eattr, evth)
            {
                RemoveMethod = new MethodDefinition($"remove_{member.Name}", mattr, voidRef)
                { Parameters = { valParm } },
                AddMethod = new MethodDefinition($"add_{member.Name}", mattr, voidRef)
                { Parameters = { valParm } }
            };
            typ.Methods.Add(evt.AddMethod);
            typ.Methods.Add(evt.RemoveMethod);
            typ.Events.Add(evt);
            if (typ.IsAbstract())
                return;
            AddDefaultEventImpl(evt.AddMethod, evt.RemoveMethod);
        }

        private static Type GetBaseType<T>(TypeDef typ)
        {
            var baseType = typeof(T);
            var baseName = (typ as IHasBase)?.Base;
            if (!string.IsNullOrWhiteSpace(baseName))
                baseType = Type.GetType(baseName, true, true);
            return baseType;
        }

        public void Dispose()
        {
        }
    }
}