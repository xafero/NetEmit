using System;

namespace NetEmit.API
{
    public class TypeDef : IComparable<TypeDef>, IComparable
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public TypeKind Kind { get; set; }

        public int CompareTo(TypeDef other) => string.CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as TypeDef);
    }
}