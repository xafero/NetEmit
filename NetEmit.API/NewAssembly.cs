﻿using System;

namespace NetEmit.API
{
    public class NewAssembly : IAssembly
    {
        public string Name { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public string FileName { get; set; }

        public bool IsExe { get; set; }
    }
}