using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {

    /// <summary>
    /// The package provided by Starcounter, containing types that are to be
    /// loaded and maintained in every host.
    /// </summary>
    public class StarcounterPackage : Package {
        private StarcounterPackage(TypeDef[] types, Stopwatch watch) : base(types, watch) {

        }

        /// <summary>
        /// Create a <see cref="StarcounterPackage"/>, governing the right set of
        /// type definitions.
        /// </summary>
        /// <param name="watch">Stop watch for diagnostics</param>
        /// <returns>The package, ready to be processed.</returns>
        public static StarcounterPackage Create(Stopwatch watch) {
            var defs = DefineTypes();
            return new StarcounterPackage(defs, watch);
        }

        static TypeDef[] DefineTypes() {
            /*             Package package = new Package(
                new TypeDef[] { //sysTableTypeDef, sysColumnTypeDef, sysIndexTypeDef, sysIndexColumnTypeDef,
                    Starcounter.Metadata.Type.CreateTypeDef(), Starcounter.Metadata.DbPrimitiveType.CreateTypeDef(), 
                    Starcounter.Metadata.MapPrimitiveType.CreateTypeDef(), ClrPrimitiveType.CreateTypeDef(),
                    Table.CreateTypeDef(), RawView.CreateTypeDef(),
                    VMView.CreateTypeDef(), ClrClass.CreateTypeDef(), 
                    Member.CreateTypeDef(), Column.CreateTypeDef(), 
                    Property.CreateTypeDef(), CodeProperty.CreateTypeDef(), MappedProperty.CreateTypeDef(),
                    Index.CreateTypeDef(), IndexedColumn.CreateTypeDef()
                },
                stopwatch_
                );*/
            return null;
        }
    }
}
