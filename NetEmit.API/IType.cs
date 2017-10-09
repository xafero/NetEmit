using NetEmit.Core;

namespace NetEmit.API
{
    public interface IType
    {
        string Name { get; set; }
        
        TypeKind Kind { get; }
    }
}