using Starcounter.XSON;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal {
    public class JsonResponseMerger {
        /// <summary>
        /// Private class that holds information about which app that registered 
        /// the callback and the callback itself.
        /// </summary>
        private class JsonMergeCallback {
            internal string sourceApp_;
            internal Func<Request, string, IEnumerable<Json>, Json> callback_;

            internal Json Invoke(Request request, string callingAppName, IEnumerable<Json> mergedJsons) {
                Json result = null;
                StarcounterEnvironment.RunWithinApplication(sourceApp_, () => {
                    result = callback_(request, callingAppName, mergedJsons);
                });
                return result;
            }
        }

        /// <summary>
        /// List of registered hooks that should be called after a merge of jsons.
        /// </summary>
        private static List<JsonMergeCallback> afterMergeCallbacks_ = new List<JsonMergeCallback>();

        /// <summary>
        /// Registers a callback that is triggered when one or more responses containing json are merged.
        /// </summary>
        /// <param name="callback"></param>
        public static void RegisterMergeCallback(Func<Request, string, IEnumerable<Json>, Json> callback) {
            afterMergeCallbacks_.Add(new JsonMergeCallback() { callback_ = callback, sourceApp_ = StarcounterEnvironment.AppName });
        }

        /// <summary>
        /// Default JSON merger function.
        /// </summary>
        internal static Response DefaultMerger(Request req, Response resp, List<Response> responses) {
            var mainResp = DoMerge(req, resp, responses);
            if (mainResp != null)
                TriggerAfterMergeCallback(req, mainResp.Resource as Json);
            
            return mainResp;
        }

        /// <summary>
        /// Default JSON merger function.
        /// </summary>
        private static Response DoMerge(Request req, Response resp, List<Response> responses) {
            Json siblingJson;
            Json mainJson;
            SiblingList stepSiblings;

            // Checking if there is only one response, which becomes the main response.
            if (resp != null) {

                mainJson = resp.Resource as Json;

                if (mainJson != null) {
                    if (mainJson.Siblings != null) {
                        if (mainJson.Siblings.HasBeenSent(mainJson.Siblings.IndexOf(mainJson))) {
                            stepSiblings = new SiblingList();
                            stepSiblings.Add(mainJson);
                            stepSiblings.MarkAsSent(0);
                            mainJson.Siblings = stepSiblings;
                        }
                    }
                    mainJson.appName = resp.AppName;
                    mainJson.wrapInAppName = true;
                }

                return resp;
            }

            var mainResponse = responses[0];
            Int32 mainResponseId = 0;

            // Searching for the current application in responses.
            for (Int32 i = 0; i < responses.Count; i++) {
                if (responses[i] == null)
                    continue;

                if (responses[i].AppName == req.HandlerOpts.CallingAppName) {

                    mainResponse = responses[i];
                    mainResponseId = i;
                    break;
                }
            }

            // Checking if its a Json response.
            mainJson = mainResponse.Resource as Json;

            if (mainJson != null) {

                mainJson.appName = mainResponse.AppName;
                mainJson.wrapInAppName = true;
                
                var oldSiblings = mainJson.Siblings;

                stepSiblings = new SiblingList();
                stepSiblings.Add(mainJson);
                mainJson.Siblings = stepSiblings;

                if (responses.Count == 1) {
                    MarkExistingSiblingsAsSent(mainJson, oldSiblings);
                    return mainResponse;
                }

                for (Int32 i = 0; i < responses.Count; i++) {

                    if (mainResponseId != i) {
                        if (responses[i] == null)
                            continue;

                        siblingJson = (Json)responses[i].Resource;

                        // TODO:
                        // Do we need to check the response in case of error and handle it or
                        // just ignore like we do now?

                        // No json in partial response. Probably because a registered handler didn't want to
                        // add anything for this uri and data.
                        if (siblingJson == null)
                            continue;

                        siblingJson.appName = responses[i].AppName;
                        siblingJson.wrapInAppName = true;

                        if (siblingJson.Siblings != null) {
                            // We have another set of step-siblings. Merge them into one list.
                            foreach (var existingSibling in siblingJson.Siblings) {
                                // TODO:
                                // Filtering out existing siblings that comes from the same app.
                                // This is a hack to avoid having multiple layouts from the launcher
                                // that gets merged, since the merger gets called a lot.
                                // Proper solution needs to be investigated.
                                // Issue: https://github.com/Starcounter/Starcounter/issues/3470
                                if (stepSiblings.ExistsForApp(existingSibling.appName))
                                    continue;
                                
                                if (!stepSiblings.Contains(existingSibling)) {
                                    stepSiblings.Add(existingSibling);
                                    existingSibling.Siblings = stepSiblings;
                                }
                            }
                        }

                        if (!stepSiblings.Contains(siblingJson)) {
                            stepSiblings.Add(siblingJson);
                        }
                        siblingJson.Siblings = stepSiblings;
                    }
                }

                MarkExistingSiblingsAsSent(mainJson, oldSiblings);
            }
            return mainResponse;
        }

        private static void TriggerAfterMergeCallback(Request request, Json json) {
            SiblingList list;
            Json newSibling = null;
            string callingAppName = request.HandlerAppName;
            if (callingAppName == null)
                callingAppName = StarcounterEnvironment.AppName;

            if (json == null || afterMergeCallbacks_.Count == 0)
                return;

            list = json.Siblings;
            if (list == null) {
                list = new SiblingList();
                list.Add(json);
            }

            foreach (var hook in afterMergeCallbacks_) {
                newSibling = hook.Invoke(request, callingAppName, list);
                if (newSibling != null) {
                    newSibling.wrapInAppName = true;
                    list.Add(newSibling);
                    newSibling.Siblings = list;
                }
            }

            if (json.Siblings == null && list.Count > 1) {
                json.Siblings = list;
            }
        }

        private static void MarkExistingSiblingsAsSent(Json mainJson, SiblingList oldSiblings) {
            SiblingList newSiblings;

            if (oldSiblings != null && mainJson.Parent != null) {
                newSiblings = mainJson.Siblings;
                for (int i = 0; i < newSiblings.Count; i++) {
                    int index = oldSiblings.IndexOf(newSiblings[i]);
                    if (index != -1) {
                        // The same sibling already exists. Lets not send it again.
                        newSiblings.MarkAsSent(index);
                    }
                }
            }
        }
    }
}
