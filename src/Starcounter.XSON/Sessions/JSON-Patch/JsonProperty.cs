using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using System;
using System.Collections.Generic;
using Starcounter.Internal;

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
            bool otEnabled = Session.Current.CheckOption(SessionOptions.PatchVersioning);
            json = root;
           
            while (pointer.MoveNext()) {
                // TODO: 
                // Check if this can be improved. Searching for transaction and execute every
                // step in a new action is not the most efficient way.
                nextIsIndex = json.Scope<JsonProperty, JsonPointer, bool, bool, bool>(
                    (prop, ptr, ot, isIndex) => {
                        prop.EvalutateCurrent(ptr, ot, ref isIndex);
                        return isIndex;
                    }, 
                    this, 
                    pointer,
                    otEnabled, 
                    nextIsIndex);
            }

        }

        private void EvalutateCurrent(JsonPointer ptr, bool otEnabled, ref bool nextIsIndex) {
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

                if (otEnabled) {
                    if (clientServerVersion != Session.Current.ServerVersion || (list._Dirty == true)) {
                        if (!list.IsValidForVersion(clientServerVersion))
                            throw new JsonPatchException("The array '" + tObjArr.TemplateName + "' in path has been replaced or removed and is no longer valid.");

                        int transformedIndex = list.TransformIndex(clientServerVersion, index);
                        if (transformedIndex == -1)
                            throw new JsonPatchException("The object at index " + index + " in array '" + tObjArr.TemplateName + "' in path has been replaced or removed and is no longer valid.");
                        index = transformedIndex;
                    }
                }
                current = list._GetAt(index);
            } else {
                var tobj = current as TObject;
                if (tobj != null) {
                    json = tobj.Getter(json);
                    if (otEnabled && !json.IsValidForVersion(Session.Current.ClientServerVersion))
                        throw new JsonPatchException("The object '" + tobj.TemplateName + "' in path has been replaced or removed and is no longer valid.");
                }
                if (json.IsArray) {
                    throw new NotImplementedException();
                }

                if (!string.IsNullOrEmpty(json._appName) && Session.Current.PublicViewModel != json) {
                    // We have a possible attachpoint. The current token in pointer points to a stepsibling.
                    // If no stepsiblings exists (or only one) it is the current json. Otherwise we need to 
                    // find the correct sibling.
                    bool found = false;
                    if (json.StepSiblings == null) {
                        if (json._appName == ptr.Current) {
                            ptr.MoveNext();
                            found = true;
                        }
                    } else {
                        foreach (Json stepSibling in json.StepSiblings) {
                            if (stepSibling._appName == ptr.Current) {
                                json = stepSibling;
                                ptr.MoveNext();
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found) {
                        throw new JsonPatchException(
                            String.Format("Unknown namespace '{0}' in path.", ptr.Current),
                            null
                        );
                    }

                    // Setting the current name to the correct app after we found an attachpoint to another app.
                    StarcounterEnvironment.AppName = json._appName;
                } 

                // Here we have moved to a stepsibling or no stepsiblings exists and the current token 
                // in the pointer points to a property.
                Template t = ((TObject)json.Template).Properties.GetExposedTemplateByName(ptr.Current);
                if (t != null) {
                    current = t;
                } else {
                    throw new JsonPatchException(
                            String.Format("Unknown property '{0}' in path.", ptr.Current),
                            null
                        );
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
