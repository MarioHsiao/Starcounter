using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using System;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.XSON {
    internal static class DefaultPatchHandler {
        internal static void Handle(Session session, JsonPatchOperation patchOp, JsonPointer pointer, IntPtr valuePtr, int valueSize) {
            string origAppName;
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (session == null) return;

            if (patchOp != JsonPatchOperation.Replace)
                throw new JsonPatchException("Unsupported patch operation in patch.");

            origAppName = StarcounterEnvironment.AppName;
            try {
                var aat = JsonProperty.Evaluate(pointer, session.PublicViewModel);

                if (!aat.Property.Editable) {
                    throw new JsonPatchException(
                        "Property '" + aat.Property.PropertyName + "' is readonly.",
                        null
                    );
                }

                aat.Json.Scope(() => {
                    if (aat.Property is TBool) {
                        ParseAndProcess((TBool)aat.Property, aat.Json, valuePtr, valueSize);
                    } else if (aat.Property is TDecimal) {
                        ParseAndProcess((TDecimal)aat.Property, aat.Json, valuePtr, valueSize);
                    } else if (aat.Property is TDouble) {
                        ParseAndProcess((TDouble)aat.Property, aat.Json, valuePtr, valueSize);
                    } else if (aat.Property is TLong) {
                        ParseAndProcess((TLong)aat.Property, aat.Json, valuePtr, valueSize);
                    } else if (aat.Property is TString) {
                        ParseAndProcess((TString)aat.Property, aat.Json, valuePtr, valueSize);
                    } else if (aat.Property is TTrigger) {
                        ParseAndProcess((TTrigger)aat.Property, aat.Json);
                    } else {
                        throw new JsonPatchException(
                            "Property " + aat.Property.TemplateName + " is invalid for userinput",
                            null
                        );
                    }
                });
            } finally {
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        private static void ParseAndProcess(TBool property, Json parent, IntPtr valuePtr, int valueSize) {
            bool value;
            int size;

            if (!JsonHelper.ParseBoolean(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsReplaced(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDecimal property, Json parent, IntPtr valuePtr, int valueSize) {
            decimal value;
            int size;

            if (!JsonHelper.ParseDecimal(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsReplaced(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDouble property, Json parent, IntPtr valuePtr, int valueSize) {
            double value;
            int size;

            if (!JsonHelper.ParseDouble(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsReplaced(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TLong property, Json parent, IntPtr valuePtr, int valueSize) {
            long value;
            int realValueSize;

            if (!JsonHelper.ParseInt(valuePtr, valueSize, out value, out realValueSize)) {
                parent.MarkAsReplaced(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TString property, Json parent, IntPtr valuePtr, int valueSize) {
            string value;
            int size;

            if (!JsonHelper.ParseString(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsReplaced(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TTrigger property, Json parent) {
            property.ProcessInput(parent);
        }

        private static string ValueAsString(IntPtr valuePtr, int valueSize) {
            string value;
            int size;
            JsonHelper.ParseString(valuePtr, valueSize, out value, out size);

            unsafe {
                byte* pval = (byte*)valuePtr;
                if (*pval == (byte)'"')
                    value = '"' + value + '"';
            }

            return value;
        }
    }
}
