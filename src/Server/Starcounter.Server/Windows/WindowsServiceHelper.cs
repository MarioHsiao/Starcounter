
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace Starcounter.Server.Windows {
    using Starcounter.Internal;
    using QUERY_SERVICE_CONFIG = Win32Service.QUERY_SERVICE_CONFIG;
    using SERVICE_STATUS_PROCESS = Win32Service.SERVICE_STATUS_PROCESS;

    public static class WindowsServiceHelper {
        /// <summary>
        /// Retreives service configuration information for the local service
        /// with the given <paramref name="serviceName"/>.
        /// </summary>
        /// <param name="serviceName">
        /// Name of the service whose configuration information we want to access.
        /// </param>
        /// <returns>
        /// Information about the given service.
        /// </returns>
        public static QUERY_SERVICE_CONFIG GetServiceConfig(string serviceName) {
            if (serviceName == null) {
                throw new ArgumentNullException("serviceName");
            }

            return GetServiceConfig(new ServiceController(serviceName));
        }

        /// <summary>
        /// Retreives service configuration information for the local service
        /// represented by <paramref name="controller"/>.
        /// </summary>
        /// <param name="controller">
        /// Controller referencing the service whose configuration information we
        /// want to access.
        /// </param>
        /// <returns>
        /// Information about the given service.
        /// </returns>
        public static QUERY_SERVICE_CONFIG GetServiceConfig(ServiceController controller) {
            return UsingService<QUERY_SERVICE_CONFIG>(
                controller,
                Win32Service.SERVICE_ACCESS.SERVICE_QUERY_CONFIG,
                DoGetServiceConfig
                );
        }

        public static SERVICE_STATUS_PROCESS GetServiceStatus(string serviceName) {
            if (serviceName == null) {
                throw new ArgumentNullException("serviceName");
            }

            return GetServiceStatus(new ServiceController(serviceName));
        }

        public static SERVICE_STATUS_PROCESS GetServiceStatus(ServiceController controller) {
            return UsingService<SERVICE_STATUS_PROCESS>(
                controller,
                Win32Service.SERVICE_ACCESS.SERVICE_QUERY_STATUS,
                DoGetServiceStatus
                );
        }

        /// <summary>
        /// Retreives command line information for the local service with
        /// the name <paramref name="serviceName"/>
        /// </summary>
        /// <remarks>
        /// The command line information of configured services is stored
        /// in the registry variable "ImagePath".
        /// </remarks>
        /// <param name="serviceName">
        /// The name of the service whose command line we want to access.
        /// </param>
        /// <returns>
        /// Command line of the service named <paramref name="serviceName"/>.
        /// </returns>
        public static string GetServiceCommandLine(string serviceName) {
            if (serviceName == null) {
                throw new ArgumentNullException("serviceName");
            }
            return GetServiceCommandLine(new ServiceController(serviceName));
        }

        /// <summary>
        /// Retreives command line information for the local service
        /// referenced by <paramref name="controller"/>
        /// </summary>
        /// <remarks>
        /// The command line information of configured services is stored
        /// in the registry variable "ImagePath".
        /// </remarks>
        /// <param name="controller">
        /// Controller referencing the service whose configuration information
        /// we want to access.
        /// </param>
        /// <returns>
        /// Command line of the service referenced by <paramref name="controller"/>.
        /// </returns>
        public static string GetServiceCommandLine(ServiceController controller) {
            if (controller == null) {
                throw new ArgumentNullException("controller");
            }
            return GetServiceConfig(controller).lpBinaryPathName;
        }

        private static QUERY_SERVICE_CONFIG DoGetServiceConfig(LocalWindowsServiceHandle service) {
            QUERY_SERVICE_CONFIG serviceConfig;
            IntPtr buffer;
            uint bufferSize;
            uint bytesNeeded;
            int lastError;
            bool br;

            serviceConfig = null;
            bufferSize = 2048;
            buffer = BitsAndBytes.Alloc((int)bufferSize);
            bytesNeeded = 0;

            try {

            call_native_query:

                br = Win32Service.QueryServiceConfig(
                    service.Handle,
                    buffer,
                    bufferSize,
                    out bytesNeeded
                    );
                if (br == false) {
                    lastError = Marshal.GetLastWin32Error();
                    if (lastError != Win32Error.ERROR_INSUFFICIENT_BUFFER)
                        throw new Win32Exception(lastError);

                    BitsAndBytes.Free(buffer);
                    bufferSize = bytesNeeded;
                    buffer = BitsAndBytes.Alloc((int)bufferSize);

                    goto call_native_query;
                }

                // Instantiate the configuration on the managed heap and
                // populate it from the buffer.

                serviceConfig = new QUERY_SERVICE_CONFIG();
                Marshal.PtrToStructure(buffer, serviceConfig);
            } finally {
                BitsAndBytes.Free(buffer);
            }

            return serviceConfig;
        }

        private static SERVICE_STATUS_PROCESS DoGetServiceStatus(LocalWindowsServiceHandle service) {
            SERVICE_STATUS_PROCESS serviceStatus;
            IntPtr buffer;
            int bufferSize;
            int bytesNeeded;
            int lastError;
            bool br;

            serviceStatus = null;
            bufferSize = Marshal.SizeOf(typeof(SERVICE_STATUS_PROCESS));
            buffer = BitsAndBytes.Alloc(bufferSize);
            bytesNeeded = 0;

            try {

            call_native_query:

                br = Win32Service.QueryServiceStatusEx(
                         service.Handle,
                         Win32Service.SC_STATUS_PROCESS_INFO,
                         buffer,
                         bufferSize,
                         out bytesNeeded
                     );

                if (br == false) {
                    lastError = Marshal.GetLastWin32Error();
                    if (lastError != Win32Error.ERROR_INSUFFICIENT_BUFFER)
                        throw new Win32Exception(lastError);

                    BitsAndBytes.Free(buffer);
                    bufferSize = bytesNeeded;
                    buffer = BitsAndBytes.Alloc((int)bufferSize);
                    goto call_native_query;
                }

                // Instantiate the status on the managed heap and
                // populate it from the buffer.

                serviceStatus = new SERVICE_STATUS_PROCESS();
                Marshal.PtrToStructure(buffer, serviceStatus);
            } finally {
                BitsAndBytes.Free(buffer);
            }

            return serviceStatus;
        }

        /// <summary>
        /// Invokes a method that needs a service handle with a certain level
        /// of access privileges, ensuring resources are freed correctly.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="serviceController">The service we open a handle to.</param>
        /// <param name="accessNeeded">The desired access.</param>
        /// <param name="target">The target to invoke.</param>
        /// <returns>A value corresponding to the type of the result.</returns>
        private static TResult UsingService<TResult>(
            ServiceController serviceController,
            Win32Service.SERVICE_ACCESS accessNeeded,
            Func<LocalWindowsServiceHandle, TResult> target) {
            if (serviceController == null) throw new ArgumentNullException("serviceController");

            using (var serviceManager = LocalWindowsServiceManager.Open(accessNeeded)) {
                using (var service = serviceManager.OpenService(serviceController.ServiceName, accessNeeded)) {
                    return target(service);
                }
            }
        }
    }
}