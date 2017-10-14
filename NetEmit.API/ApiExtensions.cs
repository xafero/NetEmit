using System;

namespace NetEmit.API
{
    public static class ApiExtensions
    {
        public static string GetExt(this AssemblyDef ass) => ass.IsExe ? "exe" : "dll";

        public static string GetKind(this AssemblyDef ass) => ass.IsExe ? "exe" : "library";

        public static string GetFileName(this AssemblyDef ass) => ass.FileName ?? $"{ass.Name}.{ass.GetExt()}";

        public static string GetVersion(this AssemblyDef ass) => ass.Version ?? $"{new Version(1, 0, 0, 0)}";

        public static Tuple<string, object> Sets(this string key, object value) => Tuple.Create(key, value);
    }
}