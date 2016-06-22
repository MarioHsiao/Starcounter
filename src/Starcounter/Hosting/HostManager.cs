using Starcounter.Binding;
using Starcounter.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {
    /// <summary>
    /// A class with a well-known static method that every database
    /// class expects to invoke once it's loaded in a host.
    /// <see cref="HostManager.InitTypeSpecification"/>
    /// </summary>
    public sealed class HostManager : IBackingHost {
        LogSource log;
        
        /// <summary>
        /// The currently installed <see cref="IBackingHost"/>.
        /// </summary>
        public static IBackingHost Host;

        static HostManager() {
            HostManager.Host = new HostManager();
        }

        private HostManager() {
            log = LogSources.Hosting;
        }
        
        /// <summary>
        /// Every database type has the resposibility to report here as
        /// soon as it's loaded in a host. This is governed by the weaver,
        /// emitting a call here from the static constructor of each such
        /// type.
        /// </summary>
        /// <remarks>
        /// Internally forwards the call to the installed backing host,
        /// referenced by <see cref="HostManager.Host"/>.
        /// </remarks>
        /// <param name="typeSpecification">The type of the type specification
        /// to initialize.</param>
        public static void InitTypeSpecification(Type typeSpecification) {
            var host = HostManager.Host;
            try {
                host.InitializeTypeSpecification(typeSpecification);
            } catch (Exception e) {
                var hostManager = host as HostManager;
                if (hostManager != null) {
                    hostManager.log.LogCritical(ExceptionFormatter.ExceptionToString(e));
                }
                throw;
            }
        }

        void IBackingHost.InitializeTypeSpecification(Type typeSpecType) {
            TypeSpecification typeSpecification;
            Type databaseType;
            TypeBinding binding;

            typeSpecification = new TypeSpecification(typeSpecType, true);
            databaseType = typeSpecType.DeclaringType;
            binding = Bindings.GetTypeBinding(databaseType.FullName);

            typeSpecification.TableHandle = binding.TableId;
            typeSpecification.TypeBinding = binding;

            var columns = binding.TypeDef.TableDef.ColumnDefs;
            for (int ci = 3; ci < columns.Length; ci++) {
                var column = columns[ci];

                if (!column.IsInherited) {
                    typeSpecification.SetColumnIndex(column.Name, ci);   
                }
            }
        }
    }
}
