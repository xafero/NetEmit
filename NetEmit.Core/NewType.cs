using System;
using NetEmit.API;
using static System.String;

namespace NetEmit.Core
{
    public abstract class NewType : IType, IComparable<NewType>, IComparable
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public abstract TypeKind Kind { get; }

        public int CompareTo(NewType other) => CompareOrdinal(Name, other.Name);

        public int CompareTo(object obj) => CompareTo(obj as NewType);
    }
}