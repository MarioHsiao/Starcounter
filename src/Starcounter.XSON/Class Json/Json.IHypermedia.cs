

using Modules;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter {
    public partial class Json : IHypermedia {



        /// <summary>
        /// Override this method to provide a custom conversion when a request
        /// is made to some other mime type than "application/json".
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public virtual string AsMimeType(MimeType mimeType) {
            return this.ToJson();
        }

        /// <summary>
        /// Override this method to provide a custom conversion when a request
        /// is made to some other mime type than "application/json".
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public virtual string AsMimeType(string mimeType) {
            return this.ToJson();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="resultingMimeType"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual byte[] AsMimeType(MimeType mimeType, out MimeType resultingMimeType, Request request = null ) {
            // A json object as a response could be the following:
            // 1) A new object not attached to a Session, in which case we just serialize it and
            //    send the response as normal json.
            // 2) A new object attached to a Session, in which case we respond with a 201 Created and
            //    set the location on how to access the object.
            // 3) Updates to a session-bound object, in which case we respond with a batch of json-patches.

            // We always start from the root object, even if the object returned from the handler is further down in the tree.
            return Starcounter_XSON.Injections._JsonMimeConverter.Convert(request,this, mimeType, out resultingMimeType);

            //throw new ArgumentException("Unknown mime type!");

            //            }
            /*            else {
                            if (root.LogChanges) {
                                // An existing sessionbound object have been updated. Return a batch of jsonpatches.
                                response = new Response() {
                                    Uncompressed = HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread)
                                };
                            }
                            else {
                                // A new sessionbound object. Return a 201 Created together with location and content.
                                if (!request.HasSession) {
                                    errorCode = request.GenerateNewSession(session);
                                    if (errorCode != 0)
                                        throw ErrorCode.ToException(errorCode);
                                }

                                request.Debug(" (new view model)");
                                root.LogChanges = true;
                                response = new Response() {
                                    Uncompressed = HttpResponseBuilder.Create201Response(root.ToJsonUtf8(), session.GetDataLocation())
                                };
                            }
                        }
            */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="resultingMimeType"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual byte[] AsMimeType(string mimeType, out MimeType resultingMimeType, Request request = null ) {
            throw new NotImplementedException();
        }

        public static implicit operator Response(Json x) {
            var response = new Response() {
                Hypermedia = x
            };
            return response;
        }

        public static implicit operator Json(Response r) {            
            return r.Hypermedia as Json;
        }
    }
}
