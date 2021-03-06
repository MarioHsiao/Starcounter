﻿using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using System;
using System.Collections.Generic;
using Starcounter.Internal;

namespace Starcounter.XSON {
    public class JsonProperty {
        private Json json;
        private object current;
        private JsonPointer pointer;
        private bool nextIsIndex;

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
            property.pointer = pointer;
            property.DoEvaluate(root);

            if (property.Property == null && (!root.IsObject)) {
                property.current = (TValue)root.Template;
            }
            return property;
        }

        private void DoEvaluate(Json root) {
            ViewModelVersion version = null;

            if (root.ChangeLog != null)
                version = root.ChangeLog.Version;
            json = root;

            try {
                while (pointer.MoveNext()) {
                    // TODO: 
                    // Check if this can be improved. Searching for transaction and execute every
                    // step in a new action is not the most efficient way.
                    json.Scope(() => {
                        this.EvalutateCurrent(version);
                    });
                }
            }catch (JsonPatchException jpex) {
                jpex.CurrentProperty = pointer.Current;
                throw;
            } catch (Exception ex) {
                var jpex = new JsonPatchException("Unhandled expection when evaluating path in patch.", ex);
                jpex.CurrentProperty = pointer.Current;
                throw jpex;
            }
        }

        private void EvalutateCurrent(ViewModelVersion version) {
            int index;
            
            if (nextIsIndex) {
                // Previous object was a Set. This token should be an index
                // to that Set. If not, it's an invalid patch.
                nextIsIndex = false;
                index = pointer.CurrentAsInt;

                var tObjArr = current as TObjArr;
                Json list = tObjArr.Getter(json);

                if (version != null) {
                    if (version.RemoteLocalVersion != version.LocalVersion || (list.dirty == true)) {
                        if (!list.IsValidForVersion(version.RemoteLocalVersion))
                            throw new JsonPatchException("The array in path has been replaced or removed and is no longer valid.");

                        int transformedIndex = list.TransformIndex(version, version.RemoteLocalVersion, index);
                        if (transformedIndex == -1)
                            throw new JsonPatchException("The object at index " + index + " for the array in path has been replaced or removed and is no longer valid.");
                        index = transformedIndex;
                    }
                }
                current = list._GetAt(index);
            } else {
                var tobj = current as TObject;
                if (tobj != null) {
                    json = tobj.Getter(json);
                    if ((version != null) && !json.IsValidForVersion(version.RemoteLocalVersion))
                        throw new JsonPatchException("The object in path has been replaced or removed and is no longer valid.");
                }
                if (json.IsArray) {
                    throw new NotImplementedException();
                }

                if (json.wrapInAppName && Session.Current.PublicViewModel != json) {
                    // We have a possible attachpoint. The current token in pointer points to a stepsibling.
                    // If no stepsiblings exists (or only one) it is the current json. Otherwise we need to 
                    // find the correct sibling.
                    bool found = false;
                    if (json.Siblings == null) {
                        if (json.appName == pointer.Current) {
                            pointer.MoveNext();
                            found = true;
                        }
                    } else {
                        foreach (Json stepSibling in json.Siblings) {
                            if (stepSibling.appName == pointer.Current) {
                                json = stepSibling;
                                pointer.MoveNext();
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found) {
                        throw new JsonPatchException(1, "Unknown namespace in path.", null);
                    }

                    // Setting the current name to the correct app after we found an attachpoint to another app.
                    StarcounterEnvironment.AppName = json.appName;
                } 

                // Here we have moved to a stepsibling or no stepsiblings exists and the current token 
                // in the pointer points to a property.
                Template t = ((TObject)json.Template).Properties.GetExposedTemplateByName(pointer.Current);
                if (t != null) {
                    current = t;
                } else {
                    throw new JsonPatchException(1, "Unknown property in path.", null);
                }
            }

            if (current is Json && !(current as Json).IsArray) {
                json = current as Json;
            } else if (current is TObjArr) {
                nextIsIndex = true;
            } else if (!(current is TObject)) {
                // Current token points to a value or an action. No more tokens should exist. 
                if (pointer.MoveNext()) {
                    throw new JsonPatchException(1, "Invalid path in patch. Property was not expected.", null);
                }
            }
        }
    }
}
