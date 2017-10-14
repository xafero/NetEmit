using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class NamespaceDef
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public ICollection<TypeDef> Types { get; set; } = new SortedSet<TypeDef>();
    }
}