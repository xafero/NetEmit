using NetEmit.API;

namespace NetEmit.Core
{
    public class NewInterface : NewType
    {
        public override TypeKind Kind => TypeKind.Interface;
    }
}