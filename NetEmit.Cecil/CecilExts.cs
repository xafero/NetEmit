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
            var constr = mod.ImportReference(type.GetConstructor(Type.EmptyTypes));
            var constrArgs = args.Except(temp).ToArray();
            var props = temp.Select(i => type.GetProperty(i.Item1)).ToArray();
            var propArgs = temp.Select(i => i.Item2).ToArray();
            bld.CustomAttributes.Add(new CustomAttribute(constr));
        }
    }
}