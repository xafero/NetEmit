using System.Collections.Generic;

namespace NetEmit.API
{
    public interface INamespace
    {
        string Name { get; set; }

        ICollection<IType> Types { get; set; }
    }
}