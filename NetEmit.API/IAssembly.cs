using System.Collections.Generic;

namespace NetEmit.API
{
    public interface IAssembly
    {
        string Name { get; set; }

        string Version { get; set; }

        string FileName { get; set; }

        bool IsExe { get; set; }

        ICollection<INamespace> Namespaces { get; set; }
    }
}