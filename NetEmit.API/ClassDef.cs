namespace NetEmit.API
{
    public class ClassDef : TypeDef, IHasBase
    {
        public ClassDef()
        {
            Kind = TypeKind.Class;
        }

        public string Base { get; set; }
    }
}