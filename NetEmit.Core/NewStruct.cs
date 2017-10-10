using NetEmit.API;

namespace NetEmit.Core
{
    public class NewStruct : NewType
    {
        public override TypeKind Kind => TypeKind.Struct;
    }
}