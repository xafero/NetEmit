using System.Collections.Generic;

namespace NetEmit.API
{
    public class InterfaceDef : TypeDef, IHasMembers
    {
        public InterfaceDef()
        {
            Kind = TypeKind.Interface;
        }

        public ICollection<MemberDef> Members { get; } = new SortedSet<MemberDef>();
    }
}