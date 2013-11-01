
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Starcounter.Server.Windows {
    /// <summary>
    /// 
    /// </summary>
    public sealed class LocalWindowsServiceManager : IDisposable {
        public IntPtr Handle { get; private set; }

        public static LocalWindowsServiceManager Open(Win32Service.SERVICE_ACCESS access) {
            IntPtr handle;

            handle = Win32Service.OpenSCManager(null, null, (uint)access);
            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return new LocalWindowsServiceManager(handle);
        }

        public LocalWindowsServiceHandle OpenService(string serviceName, Win32Service.SERVICE_ACCESS access) {
            return LocalWindowsServiceHandle.Open(this, serviceName, access);
        }

        public void Dispose() {
            Win32Service.CloseServiceHandle(this.Handle);
        }

        private LocalWindowsServiceManager(IntPtr handle) {
            this.Handle = handle;
        }
    }
}