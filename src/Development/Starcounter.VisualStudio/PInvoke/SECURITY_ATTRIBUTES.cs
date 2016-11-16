
using System;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }
}