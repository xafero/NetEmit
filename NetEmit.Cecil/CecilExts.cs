using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace NetEmit.Cecil
{
    public static class CecilExts
    {
        public static void AddAttribute<T>(this FieldDefinition bld, params object[] args) where T : Attribute
            => AddAttribute<T>(bld.Module, bld.CustomAttributes, args);

        public static void AddAttribute<T>(this MethodDefinition bld, params object[] args) where T : Attribute
            => AddAttribute<T>(bld.Module, bld.CustomAttributes, args);

        public static void AddAttribute<T>(this TypeDefinition bld, params object[] args) where T : Attribute
            => AddAttribute<T>(bld.Module, bld.CustomAttributes, args);

        public static void AddAttribute<T>(this AssemblyDefinition bld, params object[] args) where T : Attribute
            => AddAttribute<T>(bld.MainModule, bld.CustomAttributes, args);

        public static void AddAttribute<T>(ModuleDefinition mod,
            Collection<CustomAttribute> customs, params object[] args) where T : Attribute
        {
            var type = typeof(T);
            var temp = args.OfType<Tuple<string, object>>().ToArray();
            var constrArgs = args.Except(temp).ToArray();
            var constrArgsTypes = constrArgs.Select(c => c.GetType()).ToArray();
            var constr = mod.ImportReference(type.GetConstructor(constrArgsTypes));
            var props = temp.Select(i => type.GetProperty(i.Item1)).ToArray();
            var propArgs = temp.Select(i => i.Item2).ToArray();
            var attr = new CustomAttribute(constr);
            foreach (var constrArg in constrArgs)
            {
                var aa = new CustomAttributeArgument(mod.ImportReference(constrArg.GetType()), constrArg);
                attr.ConstructorArguments.Add(aa);
            }
            for (var i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                var propArg = propArgs[i];
                var aa = new CustomAttributeArgument(mod.ImportReference(prop.PropertyType), propArg);
                var pa = new CustomAttributeNamedArgument(prop.Name, aa);
                attr.Properties.Add(pa);
            }
            customs.Add(attr);
        }

        public static void AddConstructor(this TypeDefinition cla, ModuleDefinition mod,
            object body, params Tuple<string, Type>[] args)
        {
            var voidRef = mod.ImportReference(typeof(void));
            const MethodAttributes cattr = MethodAttributes.Public | MethodAttributes.HideBySig
                                           | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var cstr = new MethodDefinition(".ctor", cattr, voidRef);
            foreach (var arg in args)
                cstr.Parameters.Add(new ParameterDefinition(arg.Item1,
                    ParameterAttributes.None, mod.ImportReference(arg.Item2)));
            cla.Methods.Add(cstr);
            if (body == null)
            {
                cstr.IsRuntime = true;
                return;
            }
            var ils = cstr.Body.GetILProcessor();
            ils.Append(ils.Create(OpCodes.Ldarg_0));
            var objCstr = typeof(object).GetConstructors().First();
            ils.Append(ils.Create(OpCodes.Call, mod.ImportReference(objCstr)));
            ils.Append(ils.Create(OpCodes.Ret));
        }

        public static bool IsAbstract(this TypeDefinition typ) => typ.IsInterface | typ.IsAbstract;
    }
}