using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using NetEmit.API;

namespace NetEmit.Cecil
{
    public class CecilEmitter : IAssemblyEmitter
    {
        public string Emit(IAssembly ass)
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
                    Kind = ass.IsExe ? ModuleKind.Console : ModuleKind.Dll,
                    Runtime = TargetRuntime.Net_4_0,
                    AssemblyResolver = resolver
                };
                using (var dyn = AssemblyDefinition.CreateAssembly(assName, moduleName, parms))
                {
                    Emit(ass, dyn);
                    var wparms = new WriterParameters { WriteSymbols = false };
                    dyn.Write(file, wparms);
                }
            }
            return file;
        }

        private static void Emit(IAssembly ass, AssemblyDefinition bld)
        {
            bld.AddAttribute<CompilationRelaxationsAttribute>(8);
            bld.AddAttribute<RuntimeCompatibilityAttribute>(
                nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows).Sets(true)
            );
            var mod = bld.MainModule;
            foreach (var nsp in ass.Namespaces)
                Emit(nsp, mod);
        }

        private static void Emit(INamespace nsp, ModuleDefinition mod)
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

        private static void EmitEnum(INamespace nsp, IType typ, ModuleDefinition mod)
        {
            var enmRef = mod.ImportReference(typeof(Enum));
            var enm = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public | TypeAttributes.Sealed, enmRef);
            var underRef = mod.ImportReference(typeof(int));
            enm.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.RTSpecialName
                                                          | FieldAttributes.SpecialName, underRef));
            mod.Types.Add(enm);
        }

        private static void EmitStruct(INamespace nsp, IType typ, ModuleDefinition mod)
        {
            var valRef = mod.ImportReference(typeof(ValueType));
            var stru = new TypeDefinition(nsp.Name, typ.Name, TypeAttributes.Public
                                                              | TypeAttributes.SequentialLayout | TypeAttributes.Sealed
                                                              | TypeAttributes.BeforeFieldInit, valRef);
            mod.Types.Add(stru);
        }

        private static void EmitDelegate(INamespace nsp, IType typ, ModuleDefinition mod)
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

        private static void EmitInterface(INamespace nsp, IType typ, ModuleDefinition mod)
        {
            var intf = new TypeDefinition(nsp.Name, typ.Name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            mod.Types.Add(intf);
        }

        private static void EmitClass(INamespace nsp, IType typ, ModuleDefinition mod)
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