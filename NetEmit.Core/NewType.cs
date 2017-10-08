using System;
using NetEmit.API;

namespace NetEmit.Core
{
    public class NewType : IType
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}