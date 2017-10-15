using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NetEmit.API
{
    public static class ApiExtensions
    {
        public static string GetExt(this AssemblyDef ass) => ass.IsExe ? "exe" : "dll";

        public static string GetKind(this AssemblyDef ass) => ass.IsExe ? "exe" : "library";

        public static string GetFileName(this AssemblyDef ass) => ass.FileName ?? $"{ass.Name}.{ass.GetExt()}";

        public static string GetVersion(this AssemblyDef ass) => ass.Version ?? $"{new Version(1, 0, 0, 0)}";

        public static string GetFrameworkVersion(this AssemblyDef ass)
            => ass.Manifest.Framework ?? $"{new Version(4, 5)}";

        public static string GetFrameworkLabel(this AssemblyDef ass)
            => $".NETFramework,Version=v{ass.GetFrameworkVersion()}";

        public static string GetFrameworkName(this AssemblyDef ass)
            => $".NET Framework {ass.GetFrameworkVersion()}".TrimEnd('0').TrimEnd('.');

        public static string GetGuid(this AssemblyDef ass) => (ass.Manifest.Guid ?? Guid.NewGuid()).ToString();

        public static string GetCompany(this AssemblyDef ass) => ass.Manifest.Company ?? string.Empty;

        public static string GetConfig(this AssemblyDef ass) => ass.Manifest.Config ?? string.Empty;

        public static string GetDesc(this AssemblyDef ass) => ass.Manifest.Description ?? string.Empty;

        public static string GetTrademark(this AssemblyDef ass) => ass.Manifest.Trademark ?? string.Empty;

        public static string GetFileVersion(this AssemblyDef ass) => ass.Manifest.FileVersion ?? ass.GetVersion();

        public static string GetProduct(this AssemblyDef ass) => ass.Manifest.Product ?? ass.Name;

        public static string GetTitle(this AssemblyDef ass) => ass.Manifest.Title ?? ass.Name;

        public static string GetCopyright(this AssemblyDef ass)
            => ass.Manifest.Copyright ?? $"Copyright © {ass.GetCompany()} {DateTime.Today.Year}";

        public static CompilationRelaxations GetRelaxations(this AssemblyDef ass)
            => ass.Manifest.StringInterning ? 0 : CompilationRelaxations.NoStringInterning;

        public static bool ShouldWrapNonExceptions(this AssemblyDef ass)
            => !ass.Manifest.DontWrapNonExceptions;

        public static Tuple<string, object> Sets(this string key, object value) => Tuple.Create(key, value);

        public static IEnumerable<NamespaceDef> GetNamespaces(this IHasNamespaces par)
        {
            foreach (var nsp in par.Namespaces)
            {
                yield return new NamespaceDef
                {
                    Name = nsp.Name,
                    Types = nsp.Types
                };
                foreach (var child in nsp.GetNamespaces())
                    yield return new NamespaceDef
                    {
                        Name = $"{nsp.Name}.{child.Name}",
                        Types = child.Types
                    };
            }
        }
    }
}