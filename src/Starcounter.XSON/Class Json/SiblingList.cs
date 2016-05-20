
using System;
using System.Collections;
using System.Collections.Generic;

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
            internal set {
                Sibling sibling = list[index];

                if (value != null) {
                    value.wrapInAppName = true;
                    value.Siblings = this;
                }
                sibling.Json = value;
                sibling.HasBeenSent = false;
            }
        }
        
        public void Add(Json sibling) {
            list.Add(new Sibling() { Json = sibling });
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
    }


}
