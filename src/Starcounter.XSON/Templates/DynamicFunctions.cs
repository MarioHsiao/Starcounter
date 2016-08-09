using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal static class DynamicFunctions {
        private static readonly Dictionary<Type, Func<TValue>> getTemplateFromType = new Dictionary<Type, Func<TValue>> {
            { typeof(byte), () => { return new TLong(); } },
            { typeof(UInt16),  () => { return new TLong(); } },
            { typeof(Int16),   () => { return new TLong(); } },
            { typeof(UInt32),  () => { return new TLong(); } },
            { typeof(Int32),   () => { return new TLong(); } },
            { typeof(UInt64),  () => { return new TLong(); } },
            { typeof(Int64),   () => { return new TLong(); } },
            { typeof(float),   () => { return new TDouble(); } },
            { typeof(double),  () => { return new TDouble(); } },
            { typeof(decimal), () => { return new TDecimal(); } },
            { typeof(bool),    () => { return new TBool(); } },
            { typeof(string), () => { return new TString(); } }
        };

        private static readonly Dictionary<Type, Func<TObject, string, TValue>> addTemplateFromType = new Dictionary<Type, Func<TObject, string, TValue>> {
            { typeof(byte), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(UInt16), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(Int16), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(UInt32), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(Int32), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(UInt64), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(Int64), (TObject t, string name) => { return t.Add<TLong>(name); }},
            { typeof(float), (TObject t, string name) => { return t.Add<TDouble>(name); }},
            { typeof(double), (TObject t, string name) => { return t.Add<TDouble>(name); }},
            { typeof(decimal), (TObject t, string name) => { return t.Add<TDecimal>(name); }},
            { typeof(bool), (TObject t, string name) => { return t.Add<TBool>(name); }},
            { typeof(string), (TObject t, string name) => { return t.Add<TString>(name); }}
        };

        internal static TValue GetTemplateFromType(Type type, bool autoAddProperties = false) {
            Func<TValue> templateFunc;
            TValue result = null;

            if (getTemplateFromType.TryGetValue(type, out templateFunc)) {
                result = templateFunc();
            } else if (type.IsEnum) {
                result = new TLong();
            } else if (typeof(IEnumerable<Json>).IsAssignableFrom(type)) {
                result = new TArray<Json>();
            } else if (typeof(Json).IsAssignableFrom(type)) {
                result = new TObject();
            } else if ((typeof(IEnumerable).IsAssignableFrom(type))) {
                result = new TObjArr();
            } else { // Default is to treat it as an object template.
                result = new TObject();
            }

            if (autoAddProperties && result.TemplateTypeId == TemplateTypeEnum.Object)
                AddPropertiesFromType((TObject)result, type);

            return result;
        }

        internal static TValue AddTemplateFromType(Type type, TObject parent, string name) {
            Func<TObject, string, TValue> result;

            if (addTemplateFromType.TryGetValue(type, out result)) {
                return result(parent, name);
            }

            if (type.IsEnum) {
                return parent.Add<TLong>(name);
            }

            if (typeof(IEnumerable<Json>).IsAssignableFrom(type)) {
                return parent.Add<TArray<Json>>(name);
            }

            if (typeof(Json).IsAssignableFrom(type)) {
                return parent.Add<TObject>(name);
            }

            if ((typeof(IEnumerable).IsAssignableFrom(type))) {
                return parent.Add<TObjArr>(name);
            }

            throw new Exception(String.Format("Cannot add the {0} property to the template as the type {1} is not supported for Json properties", name, type.Name));
        }

        internal static bool IsSupportedType(Type type) {
            if (addTemplateFromType.ContainsKey(type))
                return true;
            else if (type.IsEnum)
                return true;
//            else if (typeof(IEnumerable<Json>).IsAssignableFrom(type))
//                return true;
//            else if (typeof(Json).IsAssignableFrom(type))
//                return true;
//            else if ((typeof(IEnumerable).IsAssignableFrom(type)))
//                return true;

            return false;
        }

        private static void AddPropertiesFromType(TObject template, Type dataType) {
            var props = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props) {
                if (prop.CanRead) {
                    var pt = prop.PropertyType;
                    if (DynamicFunctions.IsSupportedType(pt)) {
                        template.Add(pt, prop.Name);
                    }
                }
            }
            var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) {
                var pt = field.FieldType;
                if (DynamicFunctions.IsSupportedType(pt)) {
                    template.Add(pt, field.Name);
                }
            }
        }
    }
}
