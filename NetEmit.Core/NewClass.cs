using NetEmit.API;

namespace NetEmit.Core
{
    public class NewClass : NewType
    {
        public override TypeKind Kind => TypeKind.Class;
    }
}