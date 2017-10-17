using System.Collections.Generic;

namespace NetEmit.API
{
    public interface IHasResources
    {
        ICollection<ResourceDef> Resources { get; }
    }
}