

using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Text;
namespace Starcounter {
    public partial class Obj {

        /// <summary>
        /// In Starcounter, the user (i.e. programmer) can respond with an Obj on an Accept: text/html request.
        /// In this case, the HTML pertaining to the view of the view model described by the Obj should
        /// be retrieved. This cannot be done by the Obj itself as it does not know about the static web server
        /// or how to call any user handlers.
        /// </summary>
        public static IResponseConverter _PuppetToViewConverter = null;

        /// <summary>
        /// An Obj can be represented as a JSON object.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public byte[] AsMimeType(MimeType mimeType) {
            // A json object as a response could be the following:
            // 1) A new object not attached to a Session, in which case we just serialize it and
            //    send the response as normal json.
            // 2) A new object attached to a Session, in which case we respond with a 201 Created and
            //    set the location on how to access the object.
            // 3) Updates to a session-bound object, in which case we respond with a batch of json-patches.

            // We always start from the root object, even if the object returned from the handler is further down in the tree.
            Container r = this;
            while (r.Parent != null)
                r = r.Parent;
            Json root = (Json)r;

            //            session = Session.Current;
            //            if (session == null || session.root != root) {
            // A simple object with no serverstate. Return a 200 OK with the json as content.

            // TODO: Respect request MIME type.
            if (mimeType == MimeType.application_json) {
                return root.ToJsonUtf8();
            }
            return _PuppetToViewConverter.Convert(root, mimeType);

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

        public byte[] AsMimeType(string mimeType) {
            throw new NotImplementedException();
        }

        public static implicit operator Response(Obj x) {
            var response = new Response() {
                Hypermedia = x
            };
            return response;
        }

        public static implicit operator Obj(Response r) {            
            return r.Hypermedia as Obj;
        }
    }
}
