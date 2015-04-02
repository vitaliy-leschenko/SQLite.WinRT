// ReSharper disable CheckNamespace
// ReSharper disable AssignNullToNotNullAttribute
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

static class NativeDllHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetDllDirectory(string lpPathName);

    public static void SetDllDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var path = Path.GetDirectoryName(assembly.Location);
        path = Path.Combine(path, IntPtr.Size == 8 ? "x64" : "x86");
        SetDllDirectory(path);
    }
}
