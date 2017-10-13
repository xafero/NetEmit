using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NetEmit.Netfx
{
    public static class AssemblyExts
    {
        public static void AddAttribute<T>(this AssemblyBuilder bld, params object[] args) where T : Attribute
        {
            var type = typeof(T);
            var temp = args.OfType<Tuple<string, object>>().ToArray();
            var constr = type.GetConstructors().First();
            var constrArgs = args.Except(temp).ToArray();
            var props = temp.Select(i => type.GetProperty(i.Item1)).ToArray();
            var propArgs = temp.Select(i => i.Item2).ToArray();
            var attr = new CustomAttributeBuilder(constr, constrArgs, props, propArgs);
            bld.SetCustomAttribute(attr);
        }

        public static void AddConstructor(this TypeBuilder cla)
        {
            const MethodAttributes cattr = MethodAttributes.Public | MethodAttributes.HideBySig;
            cla.DefineDefaultConstructor(cattr);
        }
    }
}