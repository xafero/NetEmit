using System;
using System.Collections.Generic;
using NetEmit.API;

namespace NetEmit.Core
{
    public class NewNamespace : INamespace
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public ICollection<IType> Types { get; set; } = new SortedSet<IType>();
    }
}