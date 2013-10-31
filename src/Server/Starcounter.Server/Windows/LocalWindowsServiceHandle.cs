
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Starcounter.Server.Windows {
    /// <summary>
    /// 
    /// </summary>
    public sealed class LocalWindowsServiceHandle : IDisposable {
        public IntPtr Handle { get; private set; }

        public Win32Service.SERVICE_ACCESS Access { get; private set; }

        public string ServiceName { get; private set; }

        internal static LocalWindowsServiceHandle Open(
            LocalWindowsServiceManager serviceManager,
            string serviceName,
            Win32Service.SERVICE_ACCESS access) {
            return Open(serviceManager.Handle, serviceName, access);
        }

        internal static LocalWindowsServiceHandle Open(
            IntPtr serviceManagerHandle,
            string serviceName,
            Win32Service.SERVICE_ACCESS access) {
            IntPtr handle;

            handle = Win32Service.OpenService(serviceManagerHandle, serviceName, (uint)access);
            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return new LocalWindowsServiceHandle(serviceName, access, handle);
        }

        public void Dispose() {
            Win32Service.CloseServiceHandle(this.Handle);
        }

        private LocalWindowsServiceHandle(string name, Win32Service.SERVICE_ACCESS access, IntPtr handle) {
            this.Handle = handle;
            this.ServiceName = name;
            this.Access = access;
        }
    }
}
