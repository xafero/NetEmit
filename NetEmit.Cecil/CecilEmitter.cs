using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using NetEmit.API;

namespace NetEmit.Cecil
{
    public class CecilEmitter : IAssemblyEmitter
    {
        public void Emit(IAssembly ass)
        {
            var ver = Version.Parse(ass.GetVersion());
            var assName = new AssemblyNameDefinition(ass.Name, ver)
            {
                HashAlgorithm = AssemblyHashAlgorithm.SHA1
            };
            var moduleName = ass.GetFileName();
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
                    var wparms = new WriterParameters {WriteSymbols = false};
                    dyn.Write(file, wparms);
                }
            }

            Console.WriteLine(file);
        }

        private static void Emit(IAssembly ass, AssemblyDefinition bld)
        {
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
                        EmitEnum(typ, mod);
                        break;
                    case TypeKind.Struct:
                        EmitStruct(typ, mod);
                        break;
                    case TypeKind.Delegate:
                        EmitDelegate(typ, mod);
                        break;
                    case TypeKind.Interface:
                        EmitInterface(typ, mod);
                        break;
                    case TypeKind.Class:
                        EmitClass(typ, mod);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(typ.Kind.ToString());
                }
        }

        private static void EmitEnum(IType typ, ModuleDefinition mod)
        {
            var enmRef = mod.ImportReference(typeof(Enum));
            var enm = new TypeDefinition(null, typ.Name, TypeAttributes.Public | TypeAttributes.Sealed, enmRef);
            var underRef = mod.ImportReference(typeof(byte));
            enm.Fields.Add(new FieldDefinition("value__", FieldAttributes.Private | FieldAttributes.RTSpecialName
                                                          | FieldAttributes.SpecialName, underRef));
            mod.Types.Add(enm);
        }

        private static void EmitStruct(IType typ, ModuleDefinition mod)
        {
            var valRef = mod.ImportReference(typeof(ValueType));
            var stru = new TypeDefinition(null, typ.Name, TypeAttributes.Public, valRef);
            mod.Types.Add(stru);
        }

        private static void EmitDelegate(IType typ, ModuleDefinition mod)
        {
            var dlgRef = mod.ImportReference(typeof(MulticastDelegate));
            var dlg = new TypeDefinition(null, typ.Name, TypeAttributes.Public | TypeAttributes.Sealed, dlgRef);
            mod.Types.Add(dlg);
        }

        private static void EmitInterface(IType typ, ModuleDefinition mod)
        {
            var intf = new TypeDefinition(null, typ.Name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            mod.Types.Add(intf);
        }

        private static void EmitClass(IType typ, ModuleDefinition mod)
        {
            var baseRef = mod.ImportReference(typeof(object));
            var cla = new TypeDefinition(null, typ.Name, TypeAttributes.Public, baseRef);
            mod.Types.Add(cla);
        }

        public void Dispose()
        {
        }
    }
}