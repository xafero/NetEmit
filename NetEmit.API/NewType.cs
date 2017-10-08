using System;

namespace NetEmit.API
{
    public class NewType : IType
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}