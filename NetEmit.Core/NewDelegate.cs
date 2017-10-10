using NetEmit.API;

namespace NetEmit.Core
{
    public class NewDelegate : NewType
    {
        public override TypeKind Kind => TypeKind.Delegate;
    }
}