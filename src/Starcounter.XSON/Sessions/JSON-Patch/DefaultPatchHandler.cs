using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using System;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.XSON {
    internal static class DefaultPatchHandler {
        internal static void Handle(Json root, JsonPatchOperation patchOp, JsonPointer pointer, IntPtr valuePtr, int valueSize) {
            string origAppName;
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (root == null) return;

            if (patchOp != JsonPatchOperation.Replace)
                throw new JsonPatchException(1, "Unsupported patch operation in patch.");

            origAppName = StarcounterEnvironment.AppName;
            try {
                var aat = JsonProperty.Evaluate(pointer, root);

                if (aat.Property == null) {
                    throw new JsonPatchException(
                        1,
                        "Only patches for primitive values (boolean, number, string) are allowed from client.",
                        null);
                }

                if (!aat.Property.Editable) {
                    throw new JsonPatchException(
                        1,
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
                    } else {
                        throw new JsonPatchException(
                            1,
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
                parent.MarkAsDirty(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDecimal property, Json parent, IntPtr valuePtr, int valueSize) {
            decimal value;
            int size;

            if (!JsonHelper.ParseDecimal(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsDirty(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);

            if (size == 2 && value == default(decimal)) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TDouble property, Json parent, IntPtr valuePtr, int valueSize) {
            double value;
            int size;

            if (!JsonHelper.ParseDouble(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsDirty(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);

            if (size == 2 && value == default(double)) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TLong property, Json parent, IntPtr valuePtr, int valueSize) {
            long value;
            int realValueSize;

            if (!JsonHelper.ParseInt(valuePtr, valueSize, out value, out realValueSize)) {
                parent.MarkAsDirty(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            }
            property.ProcessInput(parent, value);

            if (realValueSize == 0 && value == default(long)) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TString property, Json parent, IntPtr valuePtr, int valueSize) {
            string value;
            int size;

            if (!JsonHelper.ParseString(valuePtr, valueSize, out value, out size)) {
                parent.MarkAsDirty(property);
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            }
            property.ProcessInput(parent, value);
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
