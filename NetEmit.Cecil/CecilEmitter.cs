using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Mono.Cecil;
using NetEmit.API;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
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
                    Kind = ass.IsExe() ? ModuleKind.Console : ModuleKind.Dll,
                    Runtime = TargetRuntime.Net_4_0,
                    AssemblyResolver = resolver
                };
                TargetArchitecture arch;
                if (Enum.TryParse(ass.GetArchitecture(), true, out arch))
                    parms.Architecture = arch;
                using (var dyn = AssemblyDefinition.CreateAssembly(assName, moduleName, parms))
                {
                    Emit(ass, dyn);
                    var wparms = new WriterParameters { WriteSymbols = false };
                    dyn.Write(file, wparms);
                }
            }
            return file;
        }

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
            ModuleAttributes modAttrs;
            if (Enum.TryParse(ass.GetCorFlags(), true, out modAttrs))
                mod.Attributes = modAttrs;
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
        }

        private static void EmitStruct(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var valRef = mod.ImportReference(typeof(ValueType));
            var stru = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public
                                                              | TypeAttributes.SequentialLayout | TypeAttributes.Sealed
                                                              | TypeAttributes.BeforeFieldInit, valRef);
            mod.Types.Add(stru);
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
        }

        private static void EmitClass(NamespaceDef nsp, TypeDef typ, ModuleDefinition mod)
        {
            var baseRef = mod.ImportReference(typeof(object));
            var cla = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public
                                                             | TypeAttributes.BeforeFieldInit, baseRef);
            cla.AddConstructor(mod, 1);
            mod.Types.Add(cla);
        }

        public void Dispose()
        {
        }
    }
}