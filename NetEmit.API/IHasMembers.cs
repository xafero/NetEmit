using System.Collections.Generic;

namespace NetEmit.API
{
    public interface IHasMembers
    {
        ICollection<MemberDef> Members { get; }
    }
}