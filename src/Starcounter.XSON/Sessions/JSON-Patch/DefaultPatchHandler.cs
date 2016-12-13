using System.Diagnostics;
using System.Globalization;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal static class DefaultPatchHandler {
        internal static void Handle(Json root, JsonPatchOperation patchOp, JsonPointer pointer, string value) {
            string origAppName;
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (root == null)
                return;

            if (patchOp != JsonPatchOperation.Replace)
                throw new JsonPatchException(1, "Unsupported patch operation in patch: '" + patchOp.ToString() + "'.");

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
                        ParseAndProcess((TBool)aat.Property, aat.Json, value);
                    } else if (aat.Property is TDecimal) {
                        ParseAndProcess((TDecimal)aat.Property, aat.Json, value);
                    } else if (aat.Property is TDouble) {
                        ParseAndProcess((TDouble)aat.Property, aat.Json, value);
                    } else if (aat.Property is TLong) {
                        ParseAndProcess((TLong)aat.Property, aat.Json, value);
                    } else if (aat.Property is TString) {
                        ParseAndProcess((TString)aat.Property, aat.Json, value);
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

        private static void ParseAndProcess(TBool property, Json parent, string valueStr) {
            bool value;

            if (!bool.TryParse(valueStr, out value)) {
                parent.MarkAsDirty(property);
                    ExceptionHelper.ThrowWrongValueType(null, property.PropertyName, property.JsonType, valueStr);
            }
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDecimal property, Json parent, string valueStr) {
            decimal value;
            bool markAsDirtyAfter = false;
            
            if (!decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) {
                if (string.IsNullOrEmpty(valueStr)) {
                    markAsDirtyAfter = true;
                    value = default(decimal);
                } else {
                    parent.MarkAsDirty(property);
                    ExceptionHelper.ThrowWrongValueType(null, property.PropertyName, property.JsonType, valueStr);
                }
            }
            property.ProcessInput(parent, value);

            if (markAsDirtyAfter) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TDouble property, Json parent, string valueStr) {
            double value;
            bool markAsDirtyAfter = false;

            if (!double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                if (string.IsNullOrEmpty(valueStr)) {
                    markAsDirtyAfter = true;
                    value = default(double);
                } else {
                    parent.MarkAsDirty(property);
                    ExceptionHelper.ThrowWrongValueType(null, property.PropertyName, property.JsonType, valueStr);
                }
            }
            property.ProcessInput(parent, value);

            if (markAsDirtyAfter) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TLong property, Json parent, string valueStr) {
            long value;
            bool markAsDirtyAfter = false;
            
            if (!long.TryParse(valueStr, out value)) {
                if (string.IsNullOrEmpty(valueStr)) {
                    markAsDirtyAfter = true;
                    value = default(long);
                } else {
                    parent.MarkAsDirty(property);
                    ExceptionHelper.ThrowWrongValueType(null, property.PropertyName, property.JsonType, valueStr);
                }
            }
            property.ProcessInput(parent, value);

            if (markAsDirtyAfter) {
                // Some special handling when client sends an empty string as input. We currently allow this
                // and treat it as default value for this type. However, we need to make sure we send default value
                // back to the client to properly change the string (on the client) to number.
                parent.MarkAsDirty(property);
            }
        }

        private static void ParseAndProcess(TString property, Json parent, string valueStr) {
            property.ProcessInput(parent, valueStr);
        }
    }
}
