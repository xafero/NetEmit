using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class NewAssembly : IAssembly
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public string Version { get; set; }

        public string FileName { get; set; }

        public bool IsExe { get; set; }

        public ICollection<INamespace> Namespaces { get; set; } = new SortedSet<INamespace>();
    }
}