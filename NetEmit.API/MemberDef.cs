using System;

namespace NetEmit.API
{
    public class MemberDef : IComparable<MemberDef>, IComparable
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public MemberKind Kind { get; set; }

        public int CompareTo(MemberDef other) => string.CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as MemberDef);
    }
}