using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using System;

namespace Starcounter.XSON {
    public struct JsonProperty {
        public Json Json;
        public TValue Property;
        private PointerState state;

        private struct PointerState {
            internal bool NextTokenShouldBeIndex;
            internal object Current;
            internal Json Json;
        }

        internal JsonProperty(Json json, TValue property) {
            Json = json;
            Property = property;
            state = new PointerState();
        }

        /// <summary>
        /// Evaluates the jsonpointer and retrieves the property it points to 
        /// and the correct jsonobject for the template starting from the specified root.
        /// </summary>
        /// <param name="pointer">The jsonpointer in stringformat.</param>
        /// <param name="root">The jsonobject to start from.</param>
        /// <returns></returns>
        public static JsonProperty Evaluate(string pointer, Json root) {
            return Evaluate(new JsonPointer(pointer), root);
        }

        /// <summary>
        /// Evaluates the jsonpointer and retrieves the property it points to 
        /// and the correct jsonobject for the template starting from the specified root.
        /// </summary>
        /// <param name="pointer">The jsonpointer as a bytearray in utf8.</param>
        /// <param name="root">The jsonobject to start from.</param>
        /// <returns></returns>
        public static JsonProperty Evaluate(byte[] pointer, Json root) {
            return Evaluate(new JsonPointer(pointer), root);
        }

        /// <summary>
        /// Evaluates the jsonpointer and retrieves the property it points to 
        /// and the correct jsonobject for the template starting from the specified root.
        /// </summary>
        /// <param name="pointer">The jsonpointer.</param>
        /// <param name="root">The jsonobject to start from.</param>
        /// <returns></returns>
        public static JsonProperty Evaluate(JsonPointer pointer, Json root) {
            var property = new JsonProperty();
            property.DoEvaluate(pointer, root);
            return property;
        }

        private JsonProperty DoEvaluate(JsonPointer pointer, Json root) {
            state.NextTokenShouldBeIndex = false;
            state.Current = null;
            state.Json = root;

            while (pointer.MoveNext()) {
                // TODO: 
                // Check if this can be improved. Searching for transaction and execute every
                // step in a new action is not the most efficient way.
                state.Json.AddInScope<JsonProperty, JsonPointer>((prop, ptr) => { prop.EvalutateCurrent(ptr); }, this, pointer);
            }
            return new JsonProperty(state.Json, state.Current as TValue);
        }

        private void EvalutateCurrent(JsonPointer ptr) {
            int index;
            if (state.NextTokenShouldBeIndex) {
                // Previous object was a Set. This token should be an index
                // to that Set. If not, it's an invalid patch.
                state.NextTokenShouldBeIndex = false;
                index = ptr.CurrentAsInt;
                Json list = ((TObjArr)state.Current).Getter(state.Json);
                state.Current = list._GetAt(index);
            } else {
                if (state.Current is TObject) {
                    state.Json = ((TObject)state.Current).Getter(state.Json);
                }
                if (state.Json.IsArray) {
                    throw new NotImplementedException();
                }
                Template t = ((TObject)state.Json.Template).Properties.GetExposedTemplateByName(ptr.Current);
                if (t == null) {
                    bool found = false;
                    if (state.Json.HasStepSiblings()) {
                        foreach (Json j in state.Json.GetStepSiblings()) {
                            if (j.GetAppName() == ptr.Current) {
                                state.Current = j;
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found) {
                        if (state.Json.GetAppName() == ptr.Current) {
                            state.Current = state.Json;
                        } else {
                            throw new JsonPatchException(
                                String.Format("Unknown property '{0}' in path.", ptr.Current),
                                null
                            );
                        }
                    }
                } else {
                    state.Current = t;
                }
            }

            if (state.Current is Json && !(state.Current as Json).IsArray) {
                state.Json = state.Current as Json;
            } else if (state.Current is TObjArr) {
                state.NextTokenShouldBeIndex = true;
            } else if (!(state.Current is TObject)) {
                // Current token points to a value or an action. No more tokens should exist. 
                if (ptr.MoveNext()) {
                    throw new JsonPatchException(
                                String.Format("Invalid path in patch. Property: '{0}' was not expected.", ptr.Current),
                                null
                    );
                }
            }
        }
    }
}
