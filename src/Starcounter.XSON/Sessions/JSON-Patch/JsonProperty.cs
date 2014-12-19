using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using System;

namespace Starcounter.XSON {
    public class JsonProperty {
        private Json json;
        private object current;

        private JsonProperty() {
        }

        internal JsonProperty(Json json, TValue property) {
            this.json = json;
            this.current = property;
        }

        public TValue Property {
            get { return current as TValue; }
        }

        public Json Json {
            get { return json; }
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

        private void DoEvaluate(JsonPointer pointer, Json root) {
            bool nextIsIndex = false;
            json = root;
           
            while (pointer.MoveNext()) {
                // TODO: 
                // Check if this can be improved. Searching for transaction and execute every
                // step in a new action is not the most efficient way.
                nextIsIndex = json.AddAndReturnInScope<JsonProperty, JsonPointer, bool, bool>(
                    (prop, ptr, isIndex) => {
                        prop.EvalutateCurrent(ptr, ref isIndex);
                        return isIndex;
                    }, 
                    this, 
                    pointer, 
                    nextIsIndex);
            }

        }

        private void EvalutateCurrent(JsonPointer ptr, ref bool nextIsIndex) {
            int index;
            long clientServerVersion;

            if (nextIsIndex) {
                // Previous object was a Set. This token should be an index
                // to that Set. If not, it's an invalid patch.
                nextIsIndex = false;
                index = ptr.CurrentAsInt;

                var tObjArr = current as TObjArr;
                Json list = tObjArr.Getter(json);
                clientServerVersion = Session.Current.ClientServerVersion;
                if (clientServerVersion != -1) {
                    if (!json.IsValidForVersion(clientServerVersion))
                        throw new JsonPatchException("The array '" + tObjArr.TemplateName + "' in path has been replaced or removed and is no longer valid.");

                    // TODO!
                    // OT is needed. Transform index...
                    throw new NotImplementedException("OT is needed but not implemented yet. Local version: " + Session.Current.ServerVersion + ", clients version: " + Session.Current.ClientServerVersion);
                }
                current = list._GetAt(index);
            } else {
                var tobj = current as TObject;
                if (tobj != null) {
                    json = tobj.Getter(json);
                    clientServerVersion = Session.Current.ClientServerVersion;
                    if (clientServerVersion != -1) {
                        if (!json.IsValidForVersion(clientServerVersion))
                            throw new JsonPatchException("The object '" + tobj.TemplateName + "' in path has been replaced or removed and is no longer valid.");
                    }
                }
                if (json.IsArray) {
                    throw new NotImplementedException();
                }
                Template t = ((TObject)json.Template).Properties.GetExposedTemplateByName(ptr.Current);
                if (t == null) {
                    bool found = false;
                    if (json.HasStepSiblings()) {
                        foreach (Json j in json.GetStepSiblings()) {
                            if (j.GetAppName() == ptr.Current) {
                                current = j;
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found) {
                        if (json.GetAppName() == ptr.Current) {
                            current = json;
                        } else {
                            throw new JsonPatchException(
                                String.Format("Unknown property '{0}' in path.", ptr.Current),
                                null
                            );
                        }
                    }
                } else {
                    current = t;
                }
            }

            if (current is Json && !(current as Json).IsArray) {
                json = current as Json;
            } else if (current is TObjArr) {
                nextIsIndex = true;
            } else if (!(current is TObject)) {
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
