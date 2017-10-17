using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class AssemblyDef : IHasNamespaces, IHasResources
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public string Version { get; set; }

        public string FileName { get; set; }

        public bool IsExe { get; set; }

        public bool IsGui { get; set; }

        public ICollection<ResourceDef> Resources { get; } = new SortedSet<ResourceDef>();

        public ICollection<NamespaceDef> Namespaces { get; } = new SortedSet<NamespaceDef>();

        public ManifestDef Manifest { get; set; } = new ManifestDef();
    }
}