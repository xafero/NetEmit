using System;

namespace NetEmit.Test
{
    public static class HelperFactory
    {
        public static ILHelper CreateIlHelper()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return new WindowsIlHelper();
            throw new InvalidOperationException("No helper found!");
        }
    }
}