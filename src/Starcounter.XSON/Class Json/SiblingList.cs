using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal.XSON;

namespace Starcounter.XSON {
    public class SiblingList : IEnumerable<Json> {
        private List<Sibling> list;

        /// <summary>
        /// A Sibling is a way to connect differerent viewmodels to achieve a 
        /// virtual tree that is used on the client to achieve blending.
        /// </summary>
        private class Sibling {
            internal Json Json;
            internal bool HasBeenSent;
        }

        private struct JsonEnumerator : IEnumerator<Json> {
            private List<Sibling>.Enumerator enumerator;

            internal JsonEnumerator(List<Sibling> list) {
                enumerator = list.GetEnumerator();
            }

            public Json Current {
                get {
                    var sibling = enumerator.Current;
                    if (sibling != null)
                        return sibling.Json;
                    return null;                    
                }
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public void Dispose() {
                enumerator.Dispose();
            }

            public bool MoveNext() {
                return enumerator.MoveNext();
            }

            public void Reset() {
                ((IEnumerator)enumerator).Reset();
            }
        }

        public SiblingList() {
            list = new List<Sibling>();
        }

        public Json this[int index] {
            get {
                var sibling = list[index];
                if (sibling != null)
                    return sibling.Json;
                return null;
            }
        }
        
        public void Add(Json sibling) {
            list.Add(new Sibling() { Json = sibling });
        }

        public void Remove(Json sibling) {
            int index = list.FindIndex((item) => {
                return item.Json == sibling;
            });
            if (index != -1) {
                list.RemoveAt(index);
                if (sibling.Siblings == this) {
                    sibling.wrapInAppName = false;
                    sibling.Siblings = null;
                }
            }
        }

        public bool HasBeenSent(int index) {
            return list[index].HasBeenSent;
        }

        public void MarkAsSent(int index) {
            list[index].HasBeenSent = true;
        }

        public int Count {
            get {
                return list.Count;
            }
        }

        public bool Contains(Json item) {
            return list.Exists((Sibling sibling) => {
                return (sibling.Json == item);
            });
        }

        public int IndexOf(Json item) {
            return list.FindIndex((Sibling sibling) => {
                return (sibling.Json == item);
            });
        }

        internal bool ExistsForApp(string appName) {
            return list.Exists((Sibling sibling) => {
                return (sibling.Json.appName == appName);
            });
        }

        public IEnumerator<Json> GetEnumerator() {
            return new JsonEnumerator(this.list);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new JsonEnumerator(this.list);
        }

        public void CompareAndInvalidate(SiblingList previousSiblings) {
            Sibling newSibling;
            Json prevSibling;

            // We allow only one sibling per app so we do the following checks: 
            // 1) If a sibling exist for a specific app in both new and old
            //      a) If the items are the same, mark as sent (only changed values are sent)
            //      b) If the items are not the same, mark as not sent to send whole new sibling.
            // 2) If old contains app that is not in new, send a remove (or empty object maybe if 
            //    remove is not supported.
            // 3) All items in new that does not exist in old will already be marked as not sent.

            if (previousSiblings == null)
                return;
            
            for (int i = 0; i < previousSiblings.Count; i++) {
                prevSibling = previousSiblings[i];
                
                newSibling = this.list.Find((item) => {
                    return (item.Json?.appName == prevSibling?.appName);
                });

                if (newSibling != null) {
                    newSibling.HasBeenSent = (newSibling.Json == prevSibling);
                } else {
                    // TODO:
                    // An json for the app does no longer exist. Remove from client.
                    
                }
            }
        }
    }
}
