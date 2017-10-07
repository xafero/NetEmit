namespace NetEmit.API
{
    public static class ApiExtensions
    {
        public static string GetExt(this IAssembly ass) => ass.IsExe ? "exe" : "dll";

        public static string GetKind(this IAssembly ass) => ass.IsExe ? "exe" : "library";

        public static string GetFileName(this IAssembly ass) => ass.FileName ?? $"{ass.Name}.{ass.GetExt()}";
    }
}