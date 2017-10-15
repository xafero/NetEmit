﻿using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class AssemblyDef : IHasNamespaces
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public string Version { get; set; }

        public string FileName { get; set; }

        public bool IsExe { get; set; }

        public ICollection<NamespaceDef> Namespaces { get; } = new SortedSet<NamespaceDef>();

        public ManifestDef Manifest { get; set; } = new ManifestDef();
    }
}