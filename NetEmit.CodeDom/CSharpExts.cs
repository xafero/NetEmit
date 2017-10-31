using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Noaster.Api;

namespace NetEmit.CodeDom
{
    public static class CSharpExts
    {
        public static bool IsAbstract(this IType typ) =>
            typ is IInterface | ((typ as IClass)?.Modifier.HasFlag(Modifier.Abstract) ?? false);

        private static string ToCode(this object value) =>
            value is bool ? value.ToString().ToLowerInvariant() :
            value is string ? '"' + value.ToString() + '"' : value.ToString();

        public static Assembly TryGetCompiledAssembly(this CompilerResults res)
        {
            try
            {
                return res.CompiledAssembly;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static void AddAttribute<T>(this TextWriter bld, params object[] args) where T : Attribute
        {
            var type = typeof(T);
            var temp = args.OfType<Tuple<string, object>>().ToArray();
            var constrArgs = args.Except(temp).ToArray();
            var props = temp.Select(i => type.GetProperty(i.Item1)).ToArray();
            var propArgs = temp.Select(i => i.Item2).ToArray();
            var comb = props.Zip(propArgs, Tuple.Create).Select(p => $"{p.Item1.Name} = {p.Item2.ToCode()}");
            var cstrTxt = string.Join(", ", constrArgs.Select(c => c.ToCode()));
            var propTxt = string.Join(", ", comb);
            var argTxt = string.Join(", ", cstrTxt, propTxt).Trim(',', ' ');
            bld.WriteLine($@"[assembly: {type.Name}({argTxt})]");
        }
    }
}