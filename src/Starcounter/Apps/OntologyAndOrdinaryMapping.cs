using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Advanced.XSON;
using System.Text;

namespace Starcounter {

    public class UriMapping {

        /// <summary>
        /// Registered mapping class information.
        /// </summary>
        static Dictionary<string, MappingClassInfo> classesMappingInfo_ = new Dictionary<string, MappingClassInfo>();

        /// <summary>
        /// Custom maps.
        /// </summary>
        static Dictionary<String, List<HandlerInfoForUriMapping>> customMaps_ = new Dictionary<string, List<HandlerInfoForUriMapping>>();

        /// <summary>
        /// Supported parameter string.
        /// </summary>
        const String EndsWithStringParam = "@w";

        /// <summary>
        /// Required URI mapping prefix.
        /// </summary>
        public const String MappingUriPrefix = "/sc/mapping";

        /// <summary>
        /// Mapping class URI prefix.
        /// </summary>
        public const String OntologyMappingUriPrefix = "/sc/db";

        /// <summary>
        /// Gets type name from class URI.
        /// </summary>
        static String GetClassNameFromUri(String classSubUri) {

            // Until first slash.
            Int32 slashOffset = 0;

            while (classSubUri[slashOffset] != '/') {
                slashOffset++;
            }

            // Skipping the prefix in the beginning and "/@s" at the end.
            return classSubUri.Substring(0, slashOffset);
        }

        /// <summary>
        /// Handler information for related class type.
        /// </summary>
        public class HandlerInfoForUriMapping {

            /// <summary>
            /// Class information.
            /// </summary>
            public MappingClassInfo ClassInfo;

            /// <summary>
            /// Handler ID.
            /// </summary>
            public UInt16 HandlerId;

            /// <summary>
            /// Conversion delegate to the destination class type.
            /// </summary>
            public Func<String, String> ConverterToClass;

            /// <summary>
            /// Conversion delegate from the destination class type.
            /// </summary>
            public Func<String, String> ConverterFromClass;

            /// <summary>
            /// Processed URI for handler.
            /// </summary>
            public String HandlerProcessedUri;

            /// <summary>
            /// Parameter information (offset in URI).
            /// </summary>
            public UInt16 ParamOffset;

            /// <summary>
            /// To which application this handler belongs.
            /// </summary>
            public String AppName;
        }

        /// <summary>
        /// Mapping class information.
        /// </summary>
        public class MappingClassInfo {

            /// <summary>
            /// Parent in inheritance tree.
            /// </summary>
            public MappingClassInfo Inherits;

            /// <summary>
            /// Name of the class type.
            /// </summary>
            public String Name;

            /// <summary>
            /// List of handlers.
            /// </summary>
            public List<HandlerInfoForUriMapping> Handlers;

            public MappingClassInfo(string nm) {
                Handlers = new List<HandlerInfoForUriMapping>();
                Name = nm;
                Children = new HashSet<MappingClassInfo>();
            }

            public HashSet<MappingClassInfo> Children;

            public void AddChild(MappingClassInfo n) {
                Children.Add(n);
            }
        }

        /// <summary>
        /// Mapping  handler that calls registered handlers.
        /// </summary>
        static Response MappingHandler(
            Request req,
            List<HandlerInfoForUriMapping> mappedHandlersList,
            String stringParam) {

            HandlerOptions callingHandlerOptions = req.HandlerOpts;

            // Checking if there is a substitutional handler.
            Boolean substituteHandler = false;

            if (callingHandlerOptions.SubstituteHandler != null) {
                substituteHandler = true;
            }

            // Collecting all responses in the list.
            List<Response> resps = new List<Response>();

            // Checking if there is a substitutional handler and calling it immediately.
            if (substituteHandler) {

                Response resp = new Response();

                resp = callingHandlerOptions.SubstituteHandler();
                resp.AppName = callingHandlerOptions.CallingAppName;

                resps.Add(resp);

            } else if (!String.IsNullOrEmpty(callingHandlerOptions.CallingAppName)) {

                Boolean currentAppHasHandler = false;

                // Checking if application handler is presented.
                foreach (HandlerInfoForUriMapping x in mappedHandlersList) {

                    if (x.AppName == callingHandlerOptions.CallingAppName) {

                        currentAppHasHandler = true;
                        break;
                    }
                }

                // Checking if application is not found.
                if (!currentAppHasHandler) {
                    return null;
                }
            }

            // Going through a mapped handlers list.
            foreach (HandlerInfoForUriMapping x in mappedHandlersList) {

                // Checking if we already called the substitute handler.
                if (substituteHandler) {
                    if (x.HandlerId == req.ManagedHandlerId) {
                        continue;
                    }
                }

                HandlerOptions ho = new HandlerOptions();

                // Indicating that we are calling as a proxy.
                ho.ProxyDelegateTrigger = true;

                // Setting handler id.
                ho.HandlerId = x.HandlerId;

                // Setting calling string.
                String uri = x.HandlerProcessedUri;

                String stringParamCopy = stringParam;

                // Checking if we had a parameter in handler.
                if (stringParamCopy != null) {

                    // Calling the conversion delegate.
                    if (x.ConverterFromClass != null) {

                        stringParamCopy = x.ConverterFromClass(stringParamCopy);

                        // Checking if string parameter is found after conversion.
                        if (null == stringParamCopy)
                            continue;
                    }

                    // Setting parameters info.
                    MixedCodeConstants.UserDelegateParamInfo paramInfo;
                    paramInfo.offset_ = x.ParamOffset;
                    paramInfo.len_ = (UInt16) stringParamCopy.Length;

                    // Setting parameters info.
                    ho.ParametersInfo = paramInfo;

                    // Setting calling string.
                    uri = x.HandlerProcessedUri.Replace(EndsWithStringParam, stringParamCopy);
                }

                // Calling handler.
                req.Uri = uri;
                Response resp = Self.CustomRESTRequest(req, ho);
                resps.Add(resp);
            }

            if (resps.Count > 0) {

                // Creating merged response.
                if (StarcounterEnvironment.MergeJsonSiblings) {
                    return Response.ResponsesMergerRoutine_(req, null, resps);
                }

                return resps[0];

            } else {

                // If there is no merged response - return null.
                return null;
            }
        }

        /// <summary>
        /// Maps an existing application processed URI to another URI.
        /// </summary>
        public static void Map(
            String appProcessedUri,
            String mapProcessedUri,
            String method = Handle.GET_METHOD) {

            Map(appProcessedUri, mapProcessedUri, null, null, method);
        }

        /// <summary>
        /// Maps an existing application processed URI to another URI.
        /// </summary>
        public static void Map(
            String appProcessedUri,
            String mapProcessedUri,
            Func<String, String> converterTo,
            Func<String, String> converterFrom,
            String method) {

            // Checking if method is allowed.
            if (method != Handle.GET_METHOD &&
                method != Handle.PUT_METHOD &&
                method != Handle.POST_METHOD &&
                method != Handle.PATCH_METHOD &&
                method != Handle.DELETE_METHOD) {

                throw new InvalidOperationException("HTTP method should be either GET, POST, PUT, DELETE or PATCH.");
            }

            // Checking that map URI starts with mapping prefix.
            if (!mapProcessedUri.StartsWith(MappingUriPrefix + "/", StringComparison.InvariantCultureIgnoreCase) ||
                (mapProcessedUri.Length <= MappingUriPrefix.Length + 1)) {

                throw new ArgumentException("Application can only map to handlers starting with: " + MappingUriPrefix + "/ prefix followed by some string.");
            }

            lock (customMaps_) {

                // Checking that we have only long parameter.
                Int32 numParams1 = appProcessedUri.Split('@').Length - 1,
                    numParams2 = mapProcessedUri.Split('@').Length - 1;

                if (numParams1 != numParams2) {
                    throw new ArgumentException("Application and mapping URIs have different number of parameters.");
                }

                if (numParams1 > 0) {

                    numParams1 = appProcessedUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;
                    numParams2 = mapProcessedUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;

                    if ((numParams1 != 1) || (numParams2 != 1)) {
                        throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                    }
                }

                if (numParams1 > 1) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with, at most, one parameter of type string.");
                }

                // There is always a space at the end of processed URI.
                String appProcessedMethodUriSpace = method + " " + appProcessedUri.ToLowerInvariant() + " ";

                // Searching the handler by processed URI.
                UserHandlerInfo appHandlerInfo = UriManagedHandlersCodegen.FindHandlerByProcessedUri(appProcessedMethodUriSpace,
                    new HandlerOptions());

                if (appHandlerInfo == null) {
                    throw new ArgumentException("Application handler is not registered: " + appProcessedUri);
                }

                UInt16 handlerId = appHandlerInfo.HandlerId;

                if (handlerId == HandlerOptions.InvalidUriHandlerId) {
                    throw new ArgumentException("Can not find existing handler: " + appProcessedMethodUriSpace);
                }

                // Basically creating and attaching handler to class type.
                HandlerInfoForUriMapping handler = AddHandlerToClass(
                    null,
                    handlerId,
                    appProcessedUri,
                    appProcessedMethodUriSpace,
                    appHandlerInfo.AppName,
                    converterTo,
                    converterFrom);

                // Searching for the mapped handler.
                String mapProcessedMethodUriSpace = method + " " + mapProcessedUri.ToLowerInvariant() + " ";
                UserHandlerInfo mapHandlerInfo = UriManagedHandlersCodegen.FindHandlerByProcessedUri(mapProcessedMethodUriSpace,
                    new HandlerOptions());

                // Registering the map handler if needed.
                if (null == mapHandlerInfo) {

                    Debug.Assert(false == customMaps_.ContainsKey(mapProcessedMethodUriSpace));

                    // Creating a new mapping list and adding URI to it.
                    List<HandlerInfoForUriMapping> mappedHandlersList = new List<HandlerInfoForUriMapping>();
                    mappedHandlersList.Add(handler);
                    customMaps_.Add(mapProcessedMethodUriSpace, mappedHandlersList);

                    String savedAppName = StarcounterEnvironment.AppName;
                    StarcounterEnvironment.AppName = null;

                    if (numParams1 > 0) {

                        String hs = mapProcessedUri.Replace(EndsWithStringParam, "{?}");

                        // Registering mapped URI with parameter.
                        Handle.CUSTOM(method + " " + hs, (Request req, String p) => {
                            return MappingHandler(req, mappedHandlersList, p);
                        }, new HandlerOptions() {
                            SkipHandlersPolicy = true,
                            ProxyDelegateTrigger = true,
                            TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                        });

                    } else {

                        // Registering mapped URI.
                        Handle.CUSTOM(method + " " + mapProcessedUri, (Request req) => {
                            return MappingHandler(req, mappedHandlersList, null);
                        }, new HandlerOptions() {
                            SkipHandlersPolicy = true,
                            ProxyDelegateTrigger = true,
                            TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                        });
                    }

                    StarcounterEnvironment.AppName = savedAppName;

                } else {

                    // Just adding this mapped handler to the existing list.

                    List<HandlerInfoForUriMapping> mappedList;
                    Boolean found = customMaps_.TryGetValue(mapProcessedMethodUriSpace, out mappedList);
                    Debug.Assert(true == found);

                    mappedList.Add(handler);
                }

                // Checking if we have any parameters.
                if (numParams1 > 0) {

                    // Registering the class type handler as a map to corresponding application URI.
                    String hs = appProcessedUri.Replace(EndsWithStringParam, "{?}");

                    Handle.CUSTOM(method + " " + hs, (Request req, String stringParam) => {

                        Response resp;

                        // Calling the conversion delegate.
                        if (handler.ConverterToClass != null) {

                            String convertedParam = handler.ConverterToClass(stringParam);

                            // Checking if string parameter is found after conversion.
                            if (null == convertedParam)
                                return null;

                            // Calling the mapped handler.
                            hs = mapProcessedUri.Replace(EndsWithStringParam, convertedParam);
                            req.Uri = hs;
                            resp = Self.CustomRESTRequest(req, req.HandlerOpts);

                        } else {

                            // Calling the mapped handler.
                            hs = mapProcessedUri.Replace(EndsWithStringParam, stringParam);
                            req.Uri = hs;
                            resp = Self.CustomRESTRequest(req, req.HandlerOpts);
                        }

                        return resp;

                    }, new HandlerOptions() {
                        ProxyDelegateTrigger = true,
                        TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                    });

                } else {

                    // Registering the proxy handler as a map to corresponding application URI.
                    Handle.CUSTOM(method + " " + appProcessedUri, (Request req) => {

                        Response resp;

                        // Calling the mapped handler.
                        req.Uri = mapProcessedUri;
                        resp = Self.CustomRESTRequest(req, req.HandlerOpts);

                        return resp;

                    }, new HandlerOptions() {
                        ProxyDelegateTrigger = true,
                        TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                    });
                }
            }
        }

        /// <summary>
        /// Maps an existing application processed URI to class URI.
        /// </summary>
        public static void OntologyMap(
            String appProcessedUri,
            String mappedClassInfo,
            Func<String, String> converterToClass,
            Func<String, String> converterFromClass) {

            if (!StarcounterEnvironment.OntologyMappingEnabled) {
                throw new InvalidOperationException("Ontology mapping is not enabled!");
            }

            lock (classesMappingInfo_) {

                Starcounter.Metadata.Table classMetadataTable;

                // Checking if we have just a fully namespaced database class name.
                if (!mappedClassInfo.StartsWith("/")) {

                    // Checking if fully namespaced class name is given.
                    classMetadataTable =
                        Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", mappedClassInfo).First;

                    if (null == classMetadataTable) {
                        throw new ArgumentException("Class not found: " + classMetadataTable + ". The second parameter of OntologyMap should be either a fully namespaced existing class name or /sc/db/[FullClassName]/@w.");
                    }

                    mappedClassInfo = OntologyMappingUriPrefix + "/" + mappedClassInfo + "/@w";
                }

                // Checking that we have only long parameter.
                Int32 numParams1 = appProcessedUri.Split('@').Length - 1,
                    numParams2 = mappedClassInfo.Split('@').Length - 1;

                if ((numParams1 != 1) || (numParams2 != 1)) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                }

                numParams1 = appProcessedUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;
                numParams2 = mappedClassInfo.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;

                if ((numParams1 != 1) || (numParams2 != 1)) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                }

                // There is always a space at the end of processed URI.
                String appProcessedMethodUriSpace = "GET " + appProcessedUri.ToLowerInvariant() + " ";

                // Searching the handler by processed URI.
                UserHandlerInfo handlerInfo = UriManagedHandlersCodegen.FindHandlerByProcessedUri(appProcessedMethodUriSpace,
                    new HandlerOptions());

                UInt16 handlerId = handlerInfo.HandlerId;

                if (handlerId == HandlerOptions.InvalidUriHandlerId) {
                    throw new ArgumentException("Can not find existing handler: " + appProcessedMethodUriSpace);
                }

                // NOTE: +1 is for remaining end slash.
                Int32 mappingUriPrefixLength = OntologyMappingUriPrefix.Length + 1;

                String className = GetClassNameFromUri(mappedClassInfo.Substring(mappingUriPrefixLength));

                // Getting mapped URI prefix.
                String mappedUriPrefix = mappedClassInfo.Substring(0, mappingUriPrefixLength);

                String foundFullClassName = null;

                // Assuming that full class name was specified.
                classMetadataTable = Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", className).First;

                if (classMetadataTable != null) {

                    // An arbitrary class mapping should have a full name specified.
                    foundFullClassName = classMetadataTable.FullName;

                } else {

                    throw new ArgumentException("Can not find specified database class: " + className +
                        ". Note that fully-namespaced class name has to be specified in ordinary mapping!");
                }

                // NOTE: Using database full class name because of case sensitivity.
                MappingClassInfo classInfo = null;
                classesMappingInfo_.TryGetValue(foundFullClassName, out classInfo);
                if (null == classInfo) {
                    classInfo = new MappingClassInfo(foundFullClassName);
                    classesMappingInfo_.Add(foundFullClassName, classInfo);
                }

                // Basically attaching a class handler to class.
                HandlerInfoForUriMapping handler = AddHandlerToClass(
                    classInfo,
                    handlerId,
                    appProcessedUri,
                    appProcessedMethodUriSpace,
                    handlerInfo.AppName,
                    converterToClass,
                    converterFromClass);

                // Registering the handler as a map to corresponding application URI.
                String hs = appProcessedUri.Replace(EndsWithStringParam, "{?}");
                Handle.GET(hs, (Request req, String appObjectId) => {

                    String mappedClassObjectId = appObjectId;

                    // Calling the conversion delegate.
                    if (handler.ConverterToClass != null) {

                        mappedClassObjectId = handler.ConverterToClass(appObjectId);

                        // Checking if string parameter is found after conversion.
                        if (null == mappedClassObjectId)
                            return null;
                    }

                    Response resp = Self.GET(mappedUriPrefix + className + "/" + mappedClassObjectId, null, req.HandlerOpts);

                    return resp;

                }, new HandlerOptions() {
                    ProxyDelegateTrigger = true,
                    TypeOfHandler = HandlerOptions.TypesOfHandler.OntologyMapping
                });
            }
        }

        /// <summary>
        /// Traverses from current class info up in class hierarchy, trying to find handlers.
        /// </summary>
        /// <param name="currentClassInfo">Current class from which the search starts.</param>
        /// <returns></returns>
        static MappingClassInfo FindHandlersInClassHierarchy(ref Starcounter.Metadata.Table currentClassMetadataTable) {

            // Going up in class hierarchy until we find the handlers.
            MappingClassInfo classInfo;
            classesMappingInfo_.TryGetValue(currentClassMetadataTable.FullName, out classInfo);

            if (classInfo != null)
                return classInfo;

            while (true) {

                // Going up in class inheritance tree.
                currentClassMetadataTable = currentClassMetadataTable.Inherits;

                // If there is a parent.
                if (currentClassMetadataTable != null) {

                    // Checking if there are handlers attached to this class.
                    classesMappingInfo_.TryGetValue(currentClassMetadataTable.FullName, out classInfo);

                    if (classInfo != null) {
                        break;
                    }
                } else {
                    break;
                }
            }

            return classInfo;
        }
        
        /// <summary>
        /// Calling all handlers in class hierarchy.
        /// </summary>
        static List<Response> CallAllHandlersInTypeHierarchy(Request req, String className, String paramStr) {

            // NOTE: We are searching by full name in arbitrary mapping.
            Starcounter.Metadata.Table classMetadataTable = Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", className).First;

            // Checking if class was found.
            if (classMetadataTable == null) {
                throw new ArgumentException("Can not find specified database class: " + className);
            }

            // Finding the handler based on type name.
            MappingClassInfo classInfo = FindHandlersInClassHierarchy(ref classMetadataTable);

            // Checking if handlers were found.
            if (null == classInfo) {
                throw new ArgumentException("Can not find the handlers in hierarchy for class type: " + className);
            }

            List<Response> resps = new List<Response>();

            // List of handlers that were already called in hierarchy.
            List<UInt16> alreadyCalledHandlers = new List<UInt16>();

            // Checking if we have a substitution handler.
            if (req.HandlerOpts.SubstituteHandler != null) {

                Response resp = new Response();

                resp = req.HandlerOpts.SubstituteHandler();
                resp.AppName = req.HandlerOpts.CallingAppName;

                resps.Add(resp);

                // Adding this handler since its already called.
                alreadyCalledHandlers.Add(req.ManagedHandlerId);

            } else if (!String.IsNullOrEmpty(req.HandlerOpts.CallingAppName)) {

                // NOTE: Since we are being called from an app, we need to check if
                // there is at least one handler belonging to this app in the 
                // class hierarchy.

                Boolean currentAppHasHandler = false;

                MappingClassInfo classInfoTemp = classInfo;
                Starcounter.Metadata.Table tempClassInfo = classMetadataTable;

                // Until we reach the root of the class tree.
                while (classInfoTemp != null) {

                    // Checking if application handler is presented in the hierarchy.
                    foreach (HandlerInfoForUriMapping x in classInfoTemp.Handlers) {

                        if (x.AppName == req.HandlerOpts.CallingAppName) {

                            currentAppHasHandler = true;
                            break;
                        }
                    }

                    // Checking if we found handler belonging to current application.
                    if (currentAppHasHandler)
                        break;

                    // Finding the handler based on type name.
                    tempClassInfo = tempClassInfo.Inherits;
                    classInfoTemp = FindHandlersInClassHierarchy(ref tempClassInfo);
                }

                // Checking if there are no handlers found that belong to this application.
                if (!currentAppHasHandler) {
                    return resps;
                }
            }

            // Now we have to call all existing handlers up the class hierarchy.
            while (classInfo != null) {

                CallAllHandlersForClass(resps, classInfo, paramStr, alreadyCalledHandlers);

                // Finding the handler based on type name.
                classMetadataTable = classMetadataTable.Inherits;
                classInfo = FindHandlersInClassHierarchy(ref classMetadataTable);
            }

            return resps;
        }

        /// <summary>
        /// Adding existing handler to class.
        /// </summary>
        static HandlerInfoForUriMapping AddHandlerToClass(
            MappingClassInfo classInfo,
            UInt16 handlerId,
            String handlerProcessedUri,
            String appProcessedMethodUriSpace,
            String appName,
            Func<String, String> converterToClass,
            Func<String, String> converterFromClass) {

            HandlerInfoForUriMapping x = new HandlerInfoForUriMapping();

            x.ClassInfo = classInfo;
            x.HandlerId = handlerId;
            x.HandlerProcessedUri = handlerProcessedUri;
            x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
            x.AppName = appName;
            x.ConverterToClass = converterToClass;
            x.ConverterFromClass = converterFromClass;

            if (classInfo != null) {
                classInfo.Handlers.Add(x);
            }

            /*
            // Adding handler id to the type.
            Db.Transact(() => {
                x.TheType = classInfo;
                x.HandlerId = handlerId;
                x.HandlerProcessedUri = handlerProcessedUri;
                x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
                x.AppName = appName;
                x.ConverterToClass = converterToClass;
                x.ConverterFromClass = converterFromClass;
            });*/

            return x;
        }

        /// <summary>
        /// Calling all handlers associated with a given type.
        /// </summary>
        static void CallAllHandlersForClass(
            List<Response> resps,
            MappingClassInfo type,
            String stringParam,
            List<UInt16> alreadyCalledHandlers) {

            // Processing specific handler.
            Action<HandlerInfoForUriMapping> processHandler = (HandlerInfoForUriMapping x) => {

                HandlerOptions ho = new HandlerOptions();

                // Indicating that we are calling as a proxy.
                ho.ProxyDelegateTrigger = true;

                String stringParamCopy = stringParam;

                // Calling the conversion delegate.
                if (x.ConverterFromClass != null) {

                    stringParamCopy = x.ConverterFromClass(stringParamCopy);

                    // Checking if string parameter is found after conversion.
                    if (null == stringParamCopy)
                        return;
                }

                // Setting handler id.
                ho.HandlerId = x.HandlerId;

                // Setting parameters info.
                MixedCodeConstants.UserDelegateParamInfo paramInfo;
                paramInfo.offset_ = x.ParamOffset;
                paramInfo.len_ = (UInt16) stringParamCopy.Length;

                ho.ParametersInfo = paramInfo;

                // Setting calling string.
                String uri = x.HandlerProcessedUri.Replace(EndsWithStringParam, stringParamCopy);

                // Calling handler.
                Response resp = Self.GET(uri, null, ho);
                resps.Add(resp);

            };

            foreach (HandlerInfoForUriMapping x in type.Handlers) {

                // Going through each skipped handler.
                foreach (UInt16 skipHandlerId in alreadyCalledHandlers) {
                    if (x.HandlerId == skipHandlerId) {
                        continue;
                    }
                }

                processHandler(x);

                // Adding this handler since its already called.
                alreadyCalledHandlers.Add(x.HandlerId);
            }

            /*
            foreach (HandlerInfoForUriMapping x in Db.SQL("SELECT x FROM HandlerForType x WHERE x.TheType = ?", type)) {

                // Going through each skipped handler.
                foreach (UInt16 skipHandlerId in alreadyCalledHandlers) {
                    if (x.HandlerId == skipHandlerId) {
                        continue;
                    }
                }

                processHandler(x);

                // Adding this handler since its already called.
                alreadyCalledHandlers.Add(x.HandlerId);
            }*/
        }
        
        /// <summary>
        /// Default JSON merger function.
        /// </summary>
        public static Response DefaultJsonMerger(Request req, Response resp, List<Response> responses) {
            Json siblingJson;
            Json mainJson;
            List<Json> stepSiblings;

            // Checking if there is only one response, which becomes the main response.
            
            if (resp != null) {

                mainJson = resp.Resource as Json;

                if (mainJson != null) {
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
                mainJson.StepSiblings = stepSiblings;
                stepSiblings.Add(mainJson);

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
                        siblingJson.StepSiblings = stepSiblings;

                        if (!stepSiblings.Contains(siblingJson)) {
                            stepSiblings.Add(siblingJson);
                        }
                    }
                }

                if (oldSiblings != null && mainJson.Parent != null) {
                    bool refresh = false;

                    if (oldSiblings.Count != stepSiblings.Count) {
                        refresh = true;
                    } else  {
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

        /// <summary>
        /// Handler used to call all handlers in class hierarchy.
        /// </summary>
        static Response ClassHierarchyCallProxy(Request req, String className, String paramStr) {

            // Collecting all responses in the tree.
            List<Response> resps = CallAllHandlersInTypeHierarchy(req, className, paramStr);

            if (resps.Count > 0) {

                // Creating merged response.
                if (StarcounterEnvironment.MergeJsonSiblings) {
                    return Response.ResponsesMergerRoutine_(req, null, resps);
                }

                return resps[0];

            } else {

                // If there is no merged response - return null.
                return null;
            }

        }

        /// <summary>
        /// Initializes everything needed for mapping.
        /// </summary>
        public static void Init() {

            Response.ResponsesMergerRoutine_ = DefaultJsonMerger;

            String savedAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = null;

            // Registering proxy arbitrary database classes handler.
            Handle.GET<Request, String, String>(OntologyMappingUriPrefix + "/{?}/{?}",
                (Request req, String className, String paramStr) => {
                    return ClassHierarchyCallProxy(req, className, paramStr);
                },                
                new HandlerOptions() {
                    ProxyDelegateTrigger = true,
                    TypeOfHandler = HandlerOptions.TypesOfHandler.OntologyMapping
                }
             );

            // Merges HTML partials according to provided URLs.
            Handle.GET(StarcounterConstants.HtmlMergerPrefix + "{?}", (String s) => {

                StringBuilder sb = new StringBuilder();

                String[] allPartialInfos = s.Split(new char[] { '&' });

                foreach (String appNamePlusPartialUrl in allPartialInfos) {

                    String[] a = appNamePlusPartialUrl.Split(new char[] { '=' });
                    if (String.IsNullOrEmpty(a[1]))
                        continue;

                    Response resp = Self.GET(a[1]);
                    sb.Append("<imported-template-scope scope=\"{{" + a[0] + "}}\">");
                    sb.Append(resp.Body);
                    sb.Append("</imported-template-scope>");
                }

                return sb.ToString();
            }, new HandlerOptions() {
                ProxyDelegateTrigger = true
            });

            Handle.GET(StarcounterEnvironment.Default.SystemHttpPort,
                StarcounterConstants.StarcounterSystemUriPrefix + "/" + StarcounterEnvironment.DatabaseNameLower + "/" + "GetFlag/{?}", (String flagName) => {

                    var typ = typeof(StarcounterEnvironment);
                    var flag = typ.GetField(flagName);
                    if (flag == null) {
                        return new Response() {
                            StatusDescription = "The following flag is not found: " + flagName,
                            StatusCode = 404
                        };
                    }
                    
                    return "{\"" + flagName + "\":\"" + flag.GetValue(null).ToString() + "\"}";
                });

            Handle.GET(StarcounterEnvironment.Default.SystemHttpPort,
                StarcounterConstants.StarcounterSystemUriPrefix + "/" + StarcounterEnvironment.DatabaseNameLower + "/" + "SetFlag/{?}/{?}", (String flagName, Boolean value) => {

                    var typ = typeof(StarcounterEnvironment);
                    var flag = typ.GetField(flagName);
                    if (flag == null) {
                        return new Response() {
                            StatusDescription = "The following flag is not found: " + flagName,
                            StatusCode = 404
                        };
                    }

                    flag.SetValue(null, value);
                    
                    return "{\"" + flagName + "\":\"" + flag.GetValue(null).ToString() + "\"}";
                });

            StarcounterEnvironment.AppName = savedAppName;
        }
    }
}