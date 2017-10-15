using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class NamespaceDef : IComparable<NamespaceDef>, IComparable, IHasNamespaces
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public ICollection<TypeDef> Types { get; set; } = new SortedSet<TypeDef>();

        public ICollection<NamespaceDef> Namespaces { get; } = new SortedSet<NamespaceDef>();

        public int CompareTo(NamespaceDef other) => string.CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as NamespaceDef);
    }
}