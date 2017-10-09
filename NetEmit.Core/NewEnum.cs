namespace NetEmit.Core
{
    public class NewEnum : NewType
    {
        public override TypeKind Kind => TypeKind.Enum;
    }
}