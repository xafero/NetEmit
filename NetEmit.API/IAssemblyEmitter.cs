using System;

namespace NetEmit.API
{
    public interface IAssemblyEmitter : IDisposable
    {
        void Emit(IAssembly ass);
    }
}