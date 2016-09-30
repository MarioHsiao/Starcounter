using Starcounter.Internal;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;

namespace Starcounter {

    public class UriMapping {

        /// <summary>
        /// Registered mapping class information.
        /// </summary>
        static Dictionary<string, MappingClassInfo> classesMappingInfo_ = new Dictionary<string, MappingClassInfo>();

        /// <summary>
        /// Mapped classes in different class hierarchies.
        /// </summary>
        static Dictionary<String, List<String>> mappedClassesInDifferentHierarchies_ = new Dictionary<String, List<String>>();

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
            /// Application URI for handler.
            /// </summary>
            public String AppRelativeUri;

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
            /// Name of the class type.
            /// </summary>
            public String FullClassName;

            /// <summary>
            /// List of handlers.
            /// </summary>
            public List<HandlerInfoForUriMapping> Handlers;

            /// <summary>
            /// Mapping class info.
            /// </summary>
            /// <param name="nm"></param>
            public MappingClassInfo(string nm) {
                Handlers = new List<HandlerInfoForUriMapping>();
                FullClassName = nm;
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
                String uri = x.AppRelativeUri;

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
                    uri = x.AppRelativeUri.Replace(Handle.UriParameterIndicator, stringParamCopy);
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
        /// Maps an existing application URI to another URI.
        /// </summary>
        public static void Map(
            String appUriToMap,
            String mapUri,
            String method = Handle.GET_METHOD) {

            Map(appUriToMap, mapUri, null, null, method);
        }

        /// <summary>
        /// Maps an existing application URI to another URI.
        /// </summary>
        public static void Map(
            String appUriToMap,
            String mapUri,
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
            if (!mapUri.StartsWith(MappingUriPrefix + "/", StringComparison.InvariantCultureIgnoreCase) ||
                (mapUri.Length <= MappingUriPrefix.Length + 1)) {

                throw new ArgumentException("Application can only map to handlers starting with: " + MappingUriPrefix + "/ prefix followed by some string.");
            }

            lock (customMaps_) {

                appUriToMap = appUriToMap.Replace(Handle.UriParameterIndicator, EndsWithStringParam);
                mapUri = mapUri.Replace(Handle.UriParameterIndicator, EndsWithStringParam);

                // Checking that we have only long parameter.
                Int32 numParams1 = appUriToMap.Split('@').Length - 1,
                    numParams2 = mapUri.Split('@').Length - 1;

                if (numParams1 != numParams2) {
                    throw new ArgumentException("Application and mapping URIs have different number of parameters.");
                }

                if (numParams1 > 0) {

                    numParams1 = appUriToMap.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;
                    numParams2 = mapUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;

                    if ((numParams1 != 1) || (numParams2 != 1)) {
                        throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                    }
                }

                if (numParams1 > 1) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with, at most, one parameter of type string.");
                }

                appUriToMap = appUriToMap.Replace(EndsWithStringParam, Handle.UriParameterIndicator);
                mapUri = mapUri.Replace(EndsWithStringParam, Handle.UriParameterIndicator);

                // There is always a space at the end of URI.
                String appMethodSpaceUri = method + " " + appUriToMap.ToLowerInvariant();

                // Searching the handler by URI.
                UserHandlerInfo appHandlerInfo = UriManagedHandlersCodegen.FindHandler(appMethodSpaceUri, new HandlerOptions());

                if (appHandlerInfo == null) {
                    throw new ArgumentException("Application handler is not registered: " + appUriToMap);
                }

                if (1 == numParams1) {
                    if (!appHandlerInfo.UriInfo.HasOneLastParamOfTypeString()) {
                        throw new ArgumentException("Right now mapping is only allowed for URIs with, at most, one parameter of type string.");
                    }
                }

                UInt16 handlerId = appHandlerInfo.HandlerId;

                if (handlerId == HandlerOptions.InvalidUriHandlerId) {
                    throw new ArgumentException("Can not find existing handler: " + appMethodSpaceUri);
                }

                // Basically creating and attaching handler to class type.
                HandlerInfoForUriMapping handler = AddHandlerToClass(
                    null,
                    handlerId,
                    appUriToMap,
                    appMethodSpaceUri,
                    appHandlerInfo.AppName,
                    converterTo,
                    converterFrom);

                // Searching for the mapped handler.
                String mapMethodSpaceUri = method + " " + mapUri.ToLowerInvariant();
                UserHandlerInfo mapHandlerInfo = UriManagedHandlersCodegen.FindHandler(mapMethodSpaceUri, new HandlerOptions());

                // Registering the map handler if needed.
                if (null == mapHandlerInfo) {

                    Debug.Assert(false == customMaps_.ContainsKey(mapMethodSpaceUri));

                    // Creating a new mapping list and adding URI to it.
                    List<HandlerInfoForUriMapping> mappedHandlersList = new List<HandlerInfoForUriMapping>();
                    mappedHandlersList.Add(handler);
                    customMaps_.Add(mapMethodSpaceUri, mappedHandlersList);

                    String savedAppName = StarcounterEnvironment.AppName;
                    StarcounterEnvironment.AppName = null;

                    if (numParams1 > 0) {

                        // Registering mapped URI with parameter.
                        Handle.CUSTOM(method + " " + mapUri, (Request req, String p) => {
                            return MappingHandler(req, mappedHandlersList, p);
                        }, new HandlerOptions() {
                            SkipHandlersPolicy = true,
                            ProxyDelegateTrigger = true,
                            TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                        });

                    } else {

                        // Registering mapped URI.
                        Handle.CUSTOM(method + " " + mapUri, (Request req) => {
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
                    Boolean found = customMaps_.TryGetValue(mapMethodSpaceUri, out mappedList);
                    Debug.Assert(true == found);

                    mappedList.Add(handler);
                }

                // Checking if we have any parameters.
                if (numParams1 > 0) {

                    // Registering the class type handler as a map to corresponding application URI.
                    Handle.CUSTOM(method + " " + appUriToMap, (Request req, String stringParam) => {

                        Response resp;

                        // Calling the conversion delegate.
                        if (handler.ConverterToClass != null) {

                            String convertedParam = handler.ConverterToClass(stringParam);

                            // Checking if string parameter is found after conversion.
                            if (null == convertedParam)
                                return null;

                            // Calling the mapped handler.
                            req.Uri = mapUri.Replace(Handle.UriParameterIndicator, convertedParam);
                            resp = Self.CustomRESTRequest(req, req.HandlerOpts);

                        } else {

                            // Calling the mapped handler.
                            req.Uri = mapUri.Replace(Handle.UriParameterIndicator, stringParam);
                            resp = Self.CustomRESTRequest(req, req.HandlerOpts);
                        }

                        return resp;

                    }, new HandlerOptions() {
                        ProxyDelegateTrigger = true,
                        TypeOfHandler = HandlerOptions.TypesOfHandler.OrdinaryMapping
                    });

                } else {

                    // Registering the proxy handler as a map to corresponding application URI.
                    Handle.CUSTOM(method + " " + appUriToMap, (Request req) => {

                        Response resp;

                        // Calling the mapped handler.
                        req.Uri = mapUri;
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
        /// Maps an existing application URI to class URI.
        /// </summary>
        public static void OntologyMap<T>(String appUriToMap) {

            OntologyMap(appUriToMap, typeof(T).FullName, null, null);
        }

        /// <summary>
        /// Maps an existing application URI to class URI.
        /// </summary>
        public static void OntologyMap(
            String appUriToMap,
            String mappedClassInfo) {

            OntologyMap(appUriToMap, mappedClassInfo, null, null);
        }

        /// <summary>
        /// Maps an existing application URI to class URI.
        /// </summary>
        public static void OntologyMap(
            String appUriToMap,
            String mappedClassInfo,
            Func<String, String> converterToClass,
            Func<String, String> converterFromClass) {

            if (!StarcounterEnvironment.OntologyMappingEnabled) {
                throw new InvalidOperationException("Ontology mapping is not enabled!");
            }

            lock (classesMappingInfo_) {

                Starcounter.Metadata.Table classMetadataTable;

                mappedClassInfo = mappedClassInfo.Replace(Handle.UriParameterIndicator, EndsWithStringParam);
                appUriToMap = appUriToMap.Replace(Handle.UriParameterIndicator, EndsWithStringParam);

                // Checking if we have just a fully namespaced database class name.
                if (!mappedClassInfo.StartsWith("/")) {

                    // Checking if fully namespaced class name is given.
                    classMetadataTable =
                        Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", mappedClassInfo).First;

                    if (null == classMetadataTable) {
                        throw new ArgumentException("Class not found: " + mappedClassInfo + ". The second parameter of OntologyMap should be either a fully namespaced existing class name or /sc/db/[FullClassName]/{?}.");
                    }

                    mappedClassInfo = OntologyMappingUriPrefix + "/" + mappedClassInfo + "/" + EndsWithStringParam;
                }

                // Checking that we have only long parameter.
                Int32 numParams1 = appUriToMap.Split('@').Length - 1,
                    numParams2 = mappedClassInfo.Split('@').Length - 1;

                if ((numParams1 != 1) || (numParams2 != 1)) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                }

                numParams1 = appUriToMap.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;
                numParams2 = mappedClassInfo.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;

                if ((numParams1 != 1) || (numParams2 != 1)) {
                    throw new ArgumentException("Right now ontology mapping is only allowed for URIs with one parameter of type string.");
                }

                appUriToMap = appUriToMap.Replace(EndsWithStringParam, Handle.UriParameterIndicator);
                mappedClassInfo = mappedClassInfo.Replace(EndsWithStringParam, Handle.UriParameterIndicator);

                // There is always a space at the end of URI.
                String appMethodSpaceUri = "GET " + appUriToMap.ToLowerInvariant();

                // Searching the handler by URI.
                UserHandlerInfo handlerInfo = UriManagedHandlersCodegen.FindHandler(appMethodSpaceUri, new HandlerOptions());

                if (!handlerInfo.UriInfo.HasOneLastParamOfTypeString()) {
                    throw new ArgumentException("Right now ontology mapping is only allowed for URIs with one parameter of type string.");
                }

                UInt16 handlerId = handlerInfo.HandlerId;

                if (handlerId == HandlerOptions.InvalidUriHandlerId) {
                    throw new ArgumentException("Can not find existing handler: " + appMethodSpaceUri);
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
                    appUriToMap,
                    appMethodSpaceUri,
                    handlerInfo.AppName,
                    converterToClass,
                    converterFromClass);

                // Registering the handler as a map to corresponding application URI.
                Handle.GET(appUriToMap, (Request req, String appObjectId) => {

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
        static List<Response> CallAllHandlersInTypeHierarchy(Request req, String className, String paramStr, Boolean alreadyHasResponse) {

            List<Response> resps = new List<Response>();

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
                return resps;
            }

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

                Boolean currentAppHasHandler = alreadyHasResponse;

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

                    // Checking if there is no parent.
                    if (null == tempClassInfo)
                        break;

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

                // Checking if there is no parent.
                if (null == classMetadataTable)
                    break;

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
            String appRelativeUri,
            String appMethodSpaceUri,
            String appName,
            Func<String, String> converterToClass,
            Func<String, String> converterFromClass) {

            HandlerInfoForUriMapping x = new HandlerInfoForUriMapping();

            x.ClassInfo = classInfo;
            x.HandlerId = handlerId;
            x.AppRelativeUri = appRelativeUri;
            x.ParamOffset = (UInt16)appMethodSpaceUri.IndexOf('{');
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
                x.AppMethodSpaceUri = appMethodSpaceUri;
                x.ParamOffset = (UInt16)appMethodSpaceUri.IndexOf('@');
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
                String uri = x.AppRelativeUri.Replace(Handle.UriParameterIndicator, stringParamCopy);

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
        /// Map classes in different class hierarchies.
        /// </summary>
        /// <param name="fromFullClassName">Full class name from which to map.</param>
        /// <param name="toFullClassName">Full class name to which to map.</param>
        public static void MapClassesInDifferentHierarchies(String fromFullClassName, String toFullClassName) {

            lock (classesMappingInfo_) {

                // First checking that both classes exist.
                Starcounter.Metadata.Table classMetadataTable = Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", fromFullClassName).First;

                if (null == classMetadataTable) {
                    throw new ArgumentException("Class not found: " + fromFullClassName + ". Class should exist to map it to another class.");
                }

                classMetadataTable = Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", toFullClassName).First;

                if (null == classMetadataTable) {
                    throw new ArgumentException("Class not found: " + toFullClassName + ". Class should exist to map it to another class.");
                }

                // Now checking that there is already a mapping in from class.
                List<String> mappedClassNames = null;

                // If no such mapping yet.
                if (!mappedClassesInDifferentHierarchies_.TryGetValue(fromFullClassName, out mappedClassNames)) {

                    if (!mappedClassesInDifferentHierarchies_.TryGetValue(toFullClassName, out mappedClassNames)) {
                        mappedClassNames = new List<String>();
                        mappedClassNames.Add(toFullClassName);
                        mappedClassesInDifferentHierarchies_.Add(fromFullClassName, mappedClassNames);
                        return;
                    } else {
                        // Adding existing class names.
                        mappedClassesInDifferentHierarchies_.Add(fromFullClassName, mappedClassNames);

                        // Checking if this class is not already in the list.
                        if (!mappedClassNames.Contains(fromFullClassName)) {
                            mappedClassNames.Add(fromFullClassName);
                        }
                    }
                }

                // Checking if this class is not already in the list.
                if (!mappedClassNames.Contains(toFullClassName)) {
                    mappedClassNames.Add(toFullClassName);
                }
            }
        }

        /// <summary>
        /// Handler used to call all handlers in class hierarchy.
        /// </summary>
        static Response ClassHierarchyCallProxy(Request req, String className, String paramStr) {

            // Collecting all responses in the tree.
            List<Response> resps = CallAllHandlersInTypeHierarchy(req, className, paramStr, false);

            // Getting the list of mapped classes in different hierarchies.
            List<String> mappedClassNames = null;

            // Checking if we got any responses from this class hierarchy.
            if (resps.Count > 0) {

                // Checking if there are any classes mapped from different hierarchies.
                if (mappedClassesInDifferentHierarchies_.TryGetValue(className, out mappedClassNames)) {

                    foreach (String cn in mappedClassNames) {

                        // Checking if its the same class name.
                        if (cn == className)
                            continue;

                        // Collecting all responses in the tree.
                        List<Response> otherResps = CallAllHandlersInTypeHierarchy(req, cn, paramStr, true);

                        // Adding responses to class hierarchy.
                        foreach (Response r in otherResps) {
                            resps.Add(r);
                        }
                    }
                }

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

            Response.ResponsesMergerRoutine_ = JsonResponseMerger.DefaultMerger;

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

                    Response resp = Self.GET(HttpUtility.UrlDecode(a[1]));
                    sb.Append("<imported-template-scope scope=\"" + a[0] + "\"><meta itemprop=\"juicy-composition-scope\" content==\"" + a[0] + "\"/>");
                    sb.Append(resp.Body);
                    sb.Append("</imported-template-scope>");
                }

                return sb.ToString();
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
