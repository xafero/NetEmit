using System;
using System.Collections.Generic;

namespace NetEmit.API
{
    public class TypeDef : IComparable<TypeDef>, IComparable, IHasMembers
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public TypeKind Kind { get; set; }

        public ICollection<MemberDef> Members { get; } = new SortedSet<MemberDef>();

        public int CompareTo(TypeDef other) => string.CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as TypeDef);
    }
}