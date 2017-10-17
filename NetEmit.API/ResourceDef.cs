using System;

namespace NetEmit.API
{
    public class ResourceDef : IComparable<ResourceDef>, IComparable
    {
        public string Name { get; set; }

        public int? Length { get; set; }

        public int CompareTo(ResourceDef other) => string.CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as ResourceDef);
    }
}