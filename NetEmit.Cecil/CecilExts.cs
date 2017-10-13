using System;
using System.Linq;
using Mono.Cecil;

namespace NetEmit.Cecil
{
    public static class CecilExts
    {
        public static void AddAttribute<T>(this AssemblyDefinition bld, params object[] args) where T : Attribute
        {
            var type = typeof(T);
            var temp = args.OfType<Tuple<string, object>>().ToArray();
            var mod = bld.MainModule;
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
            bld.CustomAttributes.Add(attr);
        }
    }
}