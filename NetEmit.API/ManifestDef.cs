using System;

namespace NetEmit.API
{
    public class ManifestDef
    {
        public string Architecture { get; set; }

        public string Header { get; set; }

        public bool StringInterning { get; set; }

        public bool DontWrapNonExceptions { get; set; }

        public string Company { get; set; }

        public string Config { get; set; }

        public string Copyright { get; set; }

        public string Description { get; set; }

        public string FileVersion { get; set; }

        public string Product { get; set; }

        public string Title { get; set; }

        public string Trademark { get; set; }

        public bool ComVisible { get; set; }

        public Guid? Guid { get; set; }

        public string Framework { get; set; }

        public string EntryPoint { get; set; }
    }
}