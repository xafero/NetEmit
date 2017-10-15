using System.Collections.Generic;

namespace NetEmit.API
{
    public interface IHasNamespaces
    {
        ICollection<NamespaceDef> Namespaces { get; }
    }
}