using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using NetEmit.API;

namespace NetEmit.Netfx
{
    public class AssemblyEmitter : IAssemblyEmitter
    {
        public AppDomain Domain { get; }

        public AssemblyEmitter()
        {
            Domain = AppDomain.CurrentDomain;
        }

        public void Emit(IAssembly ass)
        {
            var assName = new AssemblyName(ass.Name);
            const AssemblyBuilderAccess assAccess = AssemblyBuilderAccess.Save;
            var file = Path.GetFullPath(ass.FileName ?? $"{ass.Name}.{GetExt(ass)}");
            var dir = Path.GetDirectoryName(file);
            var dyn = Domain.DefineDynamicAssembly(assName, assAccess, dir);
            dyn.Save(Path.GetFileName(file));
        }

        private static string GetExt(IAssembly ass) => ass.IsExe ? "exe" : "dll";

        public void Dispose()
        {
            if (AppDomain.CurrentDomain == Domain)
                return;
            AppDomain.Unload(Domain);
        }

        public override string ToString() => Domain.ToString();
    }
}