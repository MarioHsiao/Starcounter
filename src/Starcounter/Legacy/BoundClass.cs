using Starcounter.Binding;
using System;
using System.Collections.Generic;

namespace Starcounter.Legacy {
    /// <summary>
    /// Represent a user defined database class that has been bound to the
    /// database.
    /// </summary>
    public sealed class BoundClass {
        // Currently backed by a type definition
        private TypeDef typeDef;

        private BoundClass() { }
        private BoundClass(TypeDef def) {
            typeDef = def;
        }
        void Init(TypeDef def) {
            typeDef = def;
        }

        /// <summary>
        /// Gets the fully qualified name of the bound class.
        /// </summary>
        public string Name {
            get {
                return typeDef.Name;
            }
        }

        /// <summary>
        /// Gets the short name of the bound class.
        /// </summary>
        public string ShortName {
            get {
                return typeDef.ShortName;
            }
        }

        /// <summary>
        /// Gets the runtime handle of this bound class. The runtime handle
        /// can be used to instantiate/insert it effectively during runtime.
        /// </summary>
        public int RuntimeHandle {
            get {
                return typeDef.TableDef.TableId;
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the current classes base
        /// bound class.
        /// </summary>
        public string BaseType {
            get {
                return typeDef.BaseName;
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the class the current bound
        /// class has defined as it's dynamic type.
        /// </summary>
        public string DynamicType {
            get {
                var index = typeDef.TypePropertyIndex;
                if (index == -1) return null;
                return typeDef.PropertyDefs[index].TargetTypeName;
            }
        }

        // Wait with this until we see if its needed.
        //public string InheritsType {
        //    get {
        //        var index = typeDef.InheritsPropertyIndex;
        //        if (index == -1) return null;
        //        return typeDef.PropertyDefs[index].TargetTypeName;
        //    }
        //}

        /// <summary>
        /// Retrive a <see cref="BoundClass"/> instance based on a fully
        /// qualified name identifier.
        /// </summary>
        /// <param name="identifier">Fully qualified name.</param>
        /// <returns>Instance representing the name.</returns>
        public static BoundClass GetClass(string identifier) {
            var def = Bindings.GetTypeDef(identifier);
            return new BoundClass(def);
        }

        /// <summary>
        /// Retreive a set of <see cref="BoundClass"/> instances based on a
        /// set of fully qualified name identifiers.
        /// </summary>
        /// <param name="identifiers">Fully qualified identifiers</param>
        /// <param name="filter">Optional filter</param>
        /// <returns>A set of <see cref="BoundClass"/> instances</returns>
        public static BoundClass[] GetClasses(string[] identifiers, Func<BoundClass, bool> filter = null) {
            filter = filter ?? BoundClass.DefaultFilter;
            var template = new BoundClass();

            var result = new List<BoundClass>();
            foreach (var identifier in identifiers) {
                var def = Bindings.GetTypeDef(identifier);
                template.Init(def);
                if (filter(template)) {
                    result.Add(new BoundClass(template.typeDef));
                }
            }

            return result.ToArray();
        }

        static bool DefaultFilter(BoundClass candidate) {
            return true;
        }
    }
}
