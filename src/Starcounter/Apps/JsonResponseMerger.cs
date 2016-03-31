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
                    if (mainJson.StepSiblings != null) {
                        stepSiblings = new SiblingList();
                        stepSiblings.Add(mainJson);

                        if (mainJson.StepSiblings.HasBeenSent(mainJson.StepSiblings.IndexOf(mainJson)))
                            stepSiblings.MarkAsSent(0);
                        mainJson.StepSiblings = stepSiblings;
                    }
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

                stepSiblings = new SiblingList();
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
                    // Old siblings exists. We need to check if we have the same ones 
                    // as before, or if some have been changed or removed to get the 
                    // correct result to the client since we don't want to send everything.

                    if (oldSiblings.Count > stepSiblings.Count) {
                        // TODO:

                        // Siblings have been removed. We need to find which ones and 
                        // if we should remove the property (i.e. namespace) from the client.
                    } else {
                        // The default setting is that siblings are sent to the client.
                        // We only check here for siblings that already exists.
                        for (int i = 0; i < oldSiblings.Count; i++) {
                            int index = stepSiblings.IndexOf(oldSiblings[i]);
                            if (index != -1) {
                                // The same sibling already exists. Lets not send it again.
                                // Updated values will be sent as usual though.
                                stepSiblings.MarkAsSent(index);
                            }
                        }
                    }
                }
            }
            return mainResponse;
        }

        private static void TriggerAfterMergeCallback(Request request, Json json) {
            SiblingList list;
            Json newSibling = null;

            if (json == null || afterMergeCallbacks_.Count == 0)
                return;

            list = json.StepSiblings;
            if (list == null) {
                list = new SiblingList();
                list.Add(json);
            }

            foreach (var hook in afterMergeCallbacks_) {
                newSibling = hook.Invoke(request, StarcounterEnvironment.AppName, list);
                if (newSibling != null) {
                    newSibling._wrapInAppName = true;
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
