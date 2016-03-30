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
            List<Json> stepSiblings;

            // Checking if there is only one response, which becomes the main response.
            if (resp != null) {

                mainJson = resp.Resource as Json;

                if (mainJson != null) {
                    mainJson.StepSiblings = null;
                    mainJson._appName = resp.AppName;
                    mainJson._wrapInAppName = true;
                }

                return resp;
            }

            var mainResponse = responses[0];
            Int32 mainResponseId = 0;

            // Searching for the current application in responses.
            for (Int32 i = 0; i < responses.Count; i++) {

                if (responses[i].AppName == req.HandlerOpts.CallingAppName) {

                    mainResponse = responses[i];
                    mainResponseId = i;
                    break;
                }
            }

            // Checking if its a Json response.
            mainJson = mainResponse.Resource as Json;

            if (mainJson != null) {

                mainJson._appName = mainResponse.AppName;
                mainJson._wrapInAppName = true;

                if (responses.Count == 1)
                    return mainResponse;
                
                var oldSiblings = mainJson.StepSiblings;

                stepSiblings = new List<Json>();
                stepSiblings.Add(mainJson);
                mainJson.StepSiblings = stepSiblings;
                
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

                        siblingJson._appName = responses[i].AppName;
                        siblingJson._wrapInAppName = true;

                        if (siblingJson.StepSiblings != null) {
                            // We have another set of step-siblings. Merge them into one list.
                            foreach (var existingSibling in siblingJson.StepSiblings) {
                                if (!stepSiblings.Contains(existingSibling)) {
                                    stepSiblings.Add(existingSibling);
                                    existingSibling.StepSiblings = stepSiblings;
                                }
                            }
                        }

                        if (!stepSiblings.Contains(siblingJson)) {
                            stepSiblings.Add(siblingJson);
                        }
                        siblingJson.StepSiblings = stepSiblings;
                    }
                }

                if (oldSiblings != null && mainJson.Parent != null) {
                    bool refresh = false;

                    if (oldSiblings.Count != stepSiblings.Count) {
                        refresh = true;
                    } else {
                        for (int i = 0; i < stepSiblings.Count; i++) {
                            if (oldSiblings[i] != stepSiblings[i]) {
                                refresh = true;
                                break;
                            }
                        }
                    }

                    // if the old siblings differ in any way from the new siblings, we refresh the whole mainjson.
                    if (refresh)
                        mainJson.Parent.MarkAsReplaced(mainJson.IndexInParent);
                }
            }

            return mainResponse;
        }

        private static void TriggerAfterMergeCallback(Request request, Json json) {
            List<Json> list;
            Json newSibling = null;

            if (json == null || afterMergeCallbacks_.Count == 0)
                return;

            list = json.StepSiblings;
            if (list == null) {
                list = new List<Json>();
                list.Add(json);
            }

            foreach (var hook in afterMergeCallbacks_) {
                newSibling = hook.Invoke(request, StarcounterEnvironment.AppName, list);
                if (newSibling != null) {
                    list.Add(newSibling);
                    newSibling.StepSiblings = list;
                }
            }

            if (json.StepSiblings == null && list.Count > 1) {
                json.StepSiblings = list;
            }
        }
    }
}
