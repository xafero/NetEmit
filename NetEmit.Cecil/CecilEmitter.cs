using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Mono.Cecil;
using NetEmit.API;
using EventAttributes = Mono.Cecil.EventAttributes;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
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
            bld.AddAttribute<ComVisibleAttribute>(ass.Manifest.ComVisible);
            bld.AddAttribute<GuidAttribute>(ass.GetGuid());
            bld.AddAttribute<TargetFrameworkAttribute>(ass.GetFrameworkLabel(),
                nameof(TargetFrameworkAttribute.FrameworkDisplayName).Sets(ass.GetFrameworkName())
            );
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
            const MethodAttributes mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                                           MethodAttributes.NewSlot | MethodAttributes.Virtual;
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
            cla.AddConstructor(mod, 1);
            mod.Types.Add(cla);
            AddMembers(mod, cla, typ);
        }

        private static void AddMembers(ModuleDefinition mod, TypeDefinition typ, IHasMembers holder)
        {
            foreach (var member in holder.Members.OfType<MethodDef>())
                AddMethod(mod, typ, member);
            foreach (var member in holder.Members.OfType<EventDef>())
                AddEvent(mod, typ, member);
            foreach (var member in holder.Members.OfType<PropertyDef>())
                AddProperty(mod, typ, member);
            foreach (var member in holder.Members.OfType<IndexerDef>())
                AddIndexer(mod, typ, member);
            foreach (var member in holder.Members.OfType<ConstantDef>())
                AddConstant(mod, typ, member);
        }

        private static void AddConstant(ModuleDefinition mod, TypeDefinition typ, ConstantDef member)
        {
            var objRef = typ.IsEnum ? typ : mod.ImportReference(typeof(object));
            const FieldAttributes attr = FieldAttributes.Public | FieldAttributes.Static
                                         | FieldAttributes.Literal;
            var constInt = typ.Fields.Count - 1;
            var fld = new FieldDefinition(member.Name, attr, objRef);
            typ.Fields.Add(fld);
            fld.Constant = constInt + 42;
        }

        private static void AddMethod(ModuleDefinition mod, TypeDefinition typ, MethodDef member)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var attr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
            if (typ.IsAbstract())
                attr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            var meth = new MethodDefinition(member.Name, attr, voidRef);
            typ.Methods.Add(meth);
        }

        private static PropertyDefinition CreateProperty(ModuleDefinition mod, string name, bool isAbstract)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var prpRef = mod.ImportReference(typeof(string));
            const PropertyAttributes pattr = PropertyAttributes.None;
            var mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName | MethodAttributes.NewSlot;
            if (isAbstract)
                mattr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
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
        }

        private static void AddEvent(ModuleDefinition mod, TypeDefinition typ, EventDef member)
        {
            var voidRef = mod.ImportReference(typeof(void));
            var evth = mod.ImportReference(typeof(EventHandler));
            const EventAttributes eattr = EventAttributes.None;
            const MethodAttributes mattr = MethodAttributes.Public | MethodAttributes.HideBySig |
                                           MethodAttributes.SpecialName | MethodAttributes.NewSlot |
                                           MethodAttributes.Abstract | MethodAttributes.Virtual;
            var valParm = new ParameterDefinition("value", ParameterAttributes.None, evth);
            var evt = new EventDefinition(member.Name, eattr, evth)
            {
                AddMethod = new MethodDefinition($"add_{member.Name}", mattr, voidRef)
                { Parameters = { valParm } },
                RemoveMethod = new MethodDefinition($"remove_{member.Name}", mattr, voidRef)
                { Parameters = { valParm } }
            };
            typ.Methods.Add(evt.AddMethod);
            typ.Methods.Add(evt.RemoveMethod);
            typ.Events.Add(evt);
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