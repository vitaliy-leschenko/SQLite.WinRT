using System;
using System.Globalization;
using System.Reflection;

namespace SQLite.WinRT
{
    internal static class Platform
    {
        private static IPlatform current;

        private const string PlatformAssemblyName = "SQLite.WinRT.Ext, Version=*, Culture=neutral, PublicKeyToken=null";
        private const string PlatformTypeFullName = "SQLite.WinRT.CurrentPlatform";

        public static IPlatform Current
        {
            get
            {
                if (current == null)
                {
                    // change name to the specified name
                    var name = PlatformTypeFullName + ", " + PlatformAssemblyName;

                    // look for the type information but do not throw if not found
                    var type = Type.GetType(name, false);

                    if (type != null)
                    {
                        // create type
                        // since we are the only one implementing this interface
                        // this cast is safe.
                        current = (IPlatform)Activator.CreateInstance(type);
                    }
                    else
                    {
                        // throw
                        ThrowForMissingPlatformAssembly();
                    }
                }
                return current;
            }
            set { current = value; }
        }

        private static void ThrowForMissingPlatformAssembly()
        {
            var portable = new AssemblyName(typeof(Platform).GetTypeInfo().Assembly.FullName);

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                "A SQLite Wrapper assembly for the current platform was not found. "+
                "Ensure that the current project references both {0} and the following platform-specific assembly: {1}.",
                portable.Name, PlatformAssemblyName));
        }
    }
}
