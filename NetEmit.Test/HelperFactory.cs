using System;

namespace NetEmit.Test
{
    public static class HelperFactory
    {
        public static ILHelper CreateIlHelper()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return new WindowsIlHelper();
                case PlatformID.Unix:
                    return new UnixIlHelper();
                default:
                    throw new InvalidOperationException("No helper found!");
            }
        }
    }
}