using System;

namespace NetEmit.API
{
    public interface IAssemblyEmitter : IDisposable
    {
        string Emit(IAssembly ass);
    }
}