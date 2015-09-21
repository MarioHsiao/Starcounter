﻿using Starcounter;
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
        /// Set to true if database class hierarchy should be emulated (unit tests).
        /// </summary>
        internal static Boolean EmulateSoDatabase = false;

        /// <summary>
        /// Global tree.
        /// </summary>
        static Tree tree_;

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

        public class Tree {
            private Dictionary<string, MappingClassInfo> nameToNodes;

            private MappingClassInfo Locate(string name) {
                if (!nameToNodes.ContainsKey(name))
                    nameToNodes.Add(name, new MappingClassInfo(name));
                return nameToNodes[name];
            }

            public void Connect(string nameFrom, string nameTo) {
                MappingClassInfo nodeFrom = Locate(nameFrom);
                MappingClassInfo nodeTo = Locate(nameTo);
                nodeTo.Inherits = nodeFrom;
                nodeFrom.AddChild(nodeTo);
            }

            public void Add(string name) {
                if (!nameToNodes.ContainsKey(name))
                    nameToNodes.Add(name, new MappingClassInfo(name));
            }

            public MappingClassInfo Find(string name) {
                if (nameToNodes.ContainsKey(name))
                    return nameToNodes[name];
                else return null;
            }

            public Tree() {
                nameToNodes = new Dictionary<string, MappingClassInfo>();
            }
        }

        private static ClrClass GetClass(string q, string p) {
            var res = Db.SQL<ClrClass>(q, p);
            if (res == null) return null;
            return res.First;
        }

        private static string ToDot(string s) {
            var p = s.LastIndexOf('.');
            if (p == -1) return s;
            return s.Substring(p + 1);
        }

        static public void WidenTree(Tree tree, string init) {
            ClrClass current = GetClass("SELECT c FROM ClrClass c WHERE FullName LIKE ?", "%." + init);
            if (current == null) return;
            tree.Add(init);
            string father = init;
            while (true) {
                if (current == null) break;
                if (current.Inherits == null) break;
                string child = father;
                father = current.Inherits.FullName;
                current = GetClass("SELECT c FROM ClrClass c where FullName=?", father);
                tree_.Connect(ToDot(father), ToDot(child));
            }
        }

        static public void GiveBigTree(Tree tree) {
            foreach (ClrClass v in Db.SQL<ClrClass>("SELECT c FROM ClrClass c")) {
                if (v.Updatable)
                    WidenTree(tree, ToDot(v.FullName));
            }
        }

        static public void ProduceLoader(MappingClassInfo node, System.IO.StreamWriter file) {
            foreach (var v in node.Children) {
                file.WriteLine("tree_.Connect(\"" + node.Name + "\", \"" + v.Name + "\");");
                file.Flush();
                ProduceLoader(v, file);
            }
        }

        /// <summary>
        /// Gets type name from class URI.
        /// </summary>
        static String GetClassNameFromUri(String soSubUri) {

            // Until first slash.
            Int32 slashOffset = 0;

            while (soSubUri[slashOffset] != '/') {
                slashOffset++;
            }

            // Skipping the prefix in the beginning and "/@s" at the end.
            return soSubUri.Substring(0, slashOffset);
        }

        /// <summary>
        /// Handler information for related class type.
        /// </summary>
        public class HandlerInfoForUriMapping {

            /// <summary>
            /// Society Objects type.
            /// </summary>
            public MappingClassInfo TheType;

            /// <summary>
            /// Handler ID.
            /// </summary>
            public UInt16 HandlerId;

            /// <summary>
            /// Conversion delegate to the destination class type.
            /// </summary>
            public Func<String, String> ConverterToSo;

            /// <summary>
            /// Conversion delegate from the destination class type.
            /// </summary>
            public Func<String, String> ConverterFromSo;

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
        /// Society Objects type.
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
                    if (x.ConverterFromSo != null) {

                        stringParamCopy = x.ConverterFromSo(stringParamCopy);

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
                        if (handler.ConverterToSo != null) {

                            String convertedParam = handler.ConverterToSo(stringParam);

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
        /// Maps an existing application processed URI to Society Objects URI.
        /// </summary>
        public static void OntologyMap(
            String appProcessedUri,
            String soProcessedUri,
            Func<String, String> converterToSo,
            Func<String, String> converterFromSo) {

            if (!StarcounterEnvironment.OntologyMappingEnabled) {
                throw new InvalidOperationException("Ontology mapping is not enabled!");
            }

            lock (tree_) {

                // Checking that we have only long parameter.
                Int32 numParams1 = appProcessedUri.Split('@').Length - 1,
                    numParams2 = soProcessedUri.Split('@').Length - 1;

                if ((numParams1 != 1) || (numParams2 != 1)) {
                    throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type string.");
                }

                numParams1 = appProcessedUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;
                numParams2 = soProcessedUri.Split(new String[] { EndsWithStringParam }, StringSplitOptions.None).Length - 1;

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

                // NOTE: By doing "/db/".Length we cover all two letters prefixes, like "/so/..." or "/db/..." etc
                String className = GetClassNameFromUri(soProcessedUri.Substring("/db/".Length));

                // Getting mapped URI prefix.
                String mappedUriPrefix = soProcessedUri.Substring(0, "/db/".Length);

                MappingClassInfo soType = null;

                if (EmulateSoDatabase) {

                    soType = tree_.Find(className);

                    if (null == soType) {
                        throw new ArgumentException("Can not find class type in hierarchy: " + className);
                    }

                } else {

                    String foundFullClassName = null;

                    // Checking if we have a Society Objects registration.
                    if (mappedUriPrefix.ToLowerInvariant() == "/so/") {

                        // Checking if there is such class in database.
                        foreach (Starcounter.Metadata.Table classInfo in
                            Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where name = ?", className)) {

                            // Checking that its a class starting with Society Objects namespace.
                            if (classInfo.FullName.ToLowerInvariant().StartsWith("concepts.ring")) {

                                foundFullClassName = classInfo.FullName;
                                break;
                            }
                        }

                        // Checking if class was not found.
                        if (null == foundFullClassName) {
                            throw new ArgumentException("Can not find specified Society Objects database class: " + className);
                        }

                    } else {

                        // Assuming that full class name was specified.
                        Starcounter.Metadata.Table classInfo =
                            Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", className).First;

                        if (classInfo != null) {

                            // An arbitrary class mapping should have a full name specified.
                            foundFullClassName = classInfo.FullName;

                        } else {

                            throw new ArgumentException("Can not find specified database class: " + className +
                                ". Note that fully-namespaced class name has to be specified in ordinary mapping!");
                        }
                    }

                    // NOTE: Using database full class name because of case sensitivity.
                    soType = tree_.Find(foundFullClassName);
                    if (null == soType) {
                        tree_.Add(foundFullClassName);
                        soType = tree_.Find(foundFullClassName);
                    }
                }

                // Basically attaching a class handler to class.
                HandlerInfoForUriMapping handler = AddHandlerToClass(
                    soType,
                    handlerId,
                    appProcessedUri,
                    appProcessedMethodUriSpace,
                    handlerInfo.AppName,
                    converterToSo,
                    converterFromSo);

                // Registering the handler as a map to corresponding application URI.
                String hs = appProcessedUri.Replace(EndsWithStringParam, "{?}");
                Handle.GET(hs, (Request req, String appObjectId) => {

                    String soObjectId = appObjectId;

                    // Calling the conversion delegate.
                    if (handler.ConverterToSo != null) {

                        soObjectId = handler.ConverterToSo(appObjectId);

                        // Checking if string parameter is found after conversion.
                        if (null == soObjectId)
                            return null;
                    }

                    // NOTE: By doing "/db/".Length we cover all two letters prefixes, like "/so/..." or "/db/..." etc
                    Response resp = Self.GET(mappedUriPrefix + className + "/" + soObjectId, null, req.HandlerOpts);

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
        static MappingClassInfo FindHandlersInClassHierarchy(ref Starcounter.Metadata.Table currentClassInfo) {

            // Going up in class hierarchy until we find the handlers.
            MappingClassInfo soType = tree_.Find(currentClassInfo.FullName);
            if (soType != null)
                return soType;

            while (true) {

                // Going up in class inheritance tree.
                currentClassInfo = currentClassInfo.Inherits;

                // If there is a parent.
                if (currentClassInfo != null) {

                    // Checking if there are handlers attached to this class.
                    soType = tree_.Find(currentClassInfo.FullName);
                    if (soType != null) {
                        break;
                    }
                } else {
                    break;
                }
            }

            return soType;
        }
        
        /// <summary>
        /// Calling all handlers in class hierarchy.
        /// </summary>
        static List<Response> CallAllHandlersInTypeHierarchy(Request req, String className, String paramStr, Boolean isSoClass) {

            // Checking if there is such class in database.
            Starcounter.Metadata.Table classInfo = null;
            MappingClassInfo soType = null;

            if (EmulateSoDatabase) {

                soType = tree_.Find(className);

            } else {

                // Checking if we have SO or arbitrary mapping.
                if (isSoClass) {

                    // Checking if there is such class in database.
                    foreach (Starcounter.Metadata.Table ci in
                        Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where name = ?", className)) {

                        // Checking that its a class starting with Society Objects namespace.
                        if (ci.FullName.ToLowerInvariant().StartsWith("concepts.ring")) {

                            classInfo = ci;
                            break;
                        }
                    }

                } else {

                    // NOTE: We are searching by full name in arbitrary mapping.
                    classInfo = Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", className).First;
                }

                // Checking if class was found.
                if (classInfo == null) {
                    throw new ArgumentException("Can not find specified database class: " + className);
                }

                // Finding the handler based on type name.
                soType = FindHandlersInClassHierarchy(ref classInfo);
            }

            // Checking if handlers were found.
            if (null == soType) {
                throw new ArgumentException("Can not find the handlers in hierarchy for class type: " + className);
            }

            List<Response> resps = new List<Response>();

            // List of handlers that were already called in hierarchy.
            List<UInt16> alreadyCalledHandlers = new List<UInt16>();

            // Checking if we have a substitutional handler.
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

                MappingClassInfo soTypeTemp = soType;
                Starcounter.Metadata.Table tempClassInfo = classInfo;

                // Until we reach the root of the class tree.
                while (soTypeTemp != null) {

                    // Checking if application handler is presented in the hierarchy.
                    foreach (HandlerInfoForUriMapping x in soTypeTemp.Handlers) {

                        if (x.AppName == req.HandlerOpts.CallingAppName) {

                            currentAppHasHandler = true;
                            break;
                        }
                    }

                    // Checking if we found handler belonging to current application.
                    if (currentAppHasHandler)
                        break;

                    if (EmulateSoDatabase) {

                        soTypeTemp = soTypeTemp.Inherits;

                    } else {

                        // Finding the handler based on type name.
                        tempClassInfo = tempClassInfo.Inherits;
                        soTypeTemp = FindHandlersInClassHierarchy(ref tempClassInfo);
                    }
                }

                // Checking if there are no handlers found that belong to this application.
                if (!currentAppHasHandler) {
                    return resps;
                }
            }

            // Now we have to call all existing handlers up the class hierarchy.
            while (soType != null) {

                CallAllHandlersForClass(resps, soType, paramStr, alreadyCalledHandlers);

                if (EmulateSoDatabase) {

                    soType = soType.Inherits;

                } else {

                    // Finding the handler based on type name.
                    classInfo = classInfo.Inherits;
                    soType = FindHandlersInClassHierarchy(ref classInfo);
                }
            }

            return resps;
        }

        /// <summary>
        /// Adding existing handler to class.
        /// </summary>
        static HandlerInfoForUriMapping AddHandlerToClass(
            MappingClassInfo soType,
            UInt16 handlerId,
            String handlerProcessedUri,
            String appProcessedMethodUriSpace,
            String appName,
            Func<String, String> converterToSo,
            Func<String, String> converterFromSo) {

            HandlerInfoForUriMapping x = new HandlerInfoForUriMapping();

            if (EmulateSoDatabase) {

                x.TheType = soType;
                x.HandlerId = handlerId;
                x.HandlerProcessedUri = handlerProcessedUri;
                x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
                x.AppName = appName;
                x.ConverterToSo = converterToSo;
                x.ConverterFromSo = converterFromSo;

                if (soType != null) {
                    soType.Handlers.Add(x);
                }

            } else {

                x.TheType = soType;
                x.HandlerId = handlerId;
                x.HandlerProcessedUri = handlerProcessedUri;
                x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
                x.AppName = appName;
                x.ConverterToSo = converterToSo;
                x.ConverterFromSo = converterFromSo;

                if (soType != null) {
                    soType.Handlers.Add(x);
                }

                /*
                // Adding handler id to the type.
                Db.Transact(() => {
                    x.TheType = soType;
                    x.HandlerId = handlerId;
                    x.HandlerProcessedUri = handlerProcessedUri;
                    x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
                    x.AppName = appName;
                    x.ConverterToSo = converterToSo;
                    x.ConverterFromSo = converterFromSo;
                });*/
            }

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
                if (x.ConverterFromSo != null) {

                    stringParamCopy = x.ConverterFromSo(stringParamCopy);

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

            if (EmulateSoDatabase) {

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

            } else {

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
                foreach (HandlerForSoType x in Db.SQL("SELECT x FROM HandlerForType x WHERE x.TheType = ?", type)) {

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
        }

        /// <summary>
        /// Populating class emulation tree.
        /// </summary>
        static void PopulateSoTree(Boolean emulateDatabase) {

            EmulateSoDatabase = emulateDatabase;

            tree_ = new Tree();

            if (EmulateSoDatabase) {

                tree_.Connect("something", "attribute");
                tree_.Connect("attribute", "role");
                tree_.Connect("role", "relation");
                tree_.Connect("relation", "participatingthing");
                tree_.Connect("participatingthing", "participant");
                tree_.Connect("participant", "responsible");
                tree_.Connect("participant", "modifier");
                tree_.Connect("participatingthing", "messagerecipient");
                tree_.Connect("participatingthing", "transfered");
                tree_.Connect("transfered", "moved");
                tree_.Connect("transfered", "traded");
                tree_.Connect("participatingthing", "modified");
                tree_.Connect("participatingthing", "inserted");
                tree_.Connect("relation", "systemusergroupmember");
                tree_.Connect("relation", "publisher");
                tree_.Connect("relation", "somebodiesrelation");
                tree_.Connect("somebodiesrelation", "subsidiary");
                tree_.Connect("somebodiesrelation", "personrole");
                tree_.Connect("personrole", "affiliatedperson");
                tree_.Connect("affiliatedperson", "employee");
                tree_.Connect("somebodiesrelation", "affiliatedorganization");
                tree_.Connect("somebodiesrelation", "consumer");
                tree_.Connect("somebodiesrelation", "creator");
                tree_.Connect("relation", "personcategorymember");
                tree_.Connect("relation", "companycategorymember");
                tree_.Connect("relation", "vendible");
                tree_.Connect("relation", "address");
                tree_.Connect("address", "digitaladdress");
                tree_.Connect("digitaladdress", "url");
                tree_.Connect("digitaladdress", "uripath");
                tree_.Connect("digitaladdress", "uri");
                tree_.Connect("digitaladdress", "port");
                tree_.Connect("digitaladdress", "emailaddress");
                tree_.Connect("digitaladdress", "internetaddress");
                tree_.Connect("address", "telephonenumber");
                tree_.Connect("address", "nowhere");
                tree_.Connect("relation", "addressrelation");
                tree_.Connect("addressrelation", "abstractcrossreference");
                tree_.Connect("abstractcrossreference", "hyperlink");
                tree_.Connect("addressrelation", "webaddress");
                tree_.Connect("webaddress", "homepageurl");
                tree_.Connect("addressrelation", "emailrelation");
                tree_.Connect("addressrelation", "telephonenumberrelation");
                tree_.Connect("telephonenumberrelation", "officephonenumber");
                tree_.Connect("telephonenumberrelation", "mobilephonenumber");
                tree_.Connect("telephonenumberrelation", "homephonenumber");
                tree_.Connect("telephonenumberrelation", "faxphonenumber");
                tree_.Connect("addressrelation", "placement");
                tree_.Connect("placement", "negativeplacement");
                tree_.Connect("placement", "positiveplacement");
                tree_.Connect("relation", "definedquantification");
                tree_.Connect("relation", "groupmember");
                tree_.Connect("relation", "eventrole");
                tree_.Connect("eventrole", "eventsubset");
                tree_.Connect("eventsubset", "eventshare");
                tree_.Connect("relation", "wordsense");
                tree_.Connect("role", "systemuser");
                tree_.Connect("role", "contentbearingobject");
                tree_.Connect("contentbearingobject", "digitalasset");
                tree_.Connect("digitalasset", "softwareapplication");
                tree_.Connect("role", "somebody");
                tree_.Connect("somebody", "legalentity");
                tree_.Connect("legalentity", "organization");
                tree_.Connect("organization", "company");
                tree_.Connect("legalentity", "person");
                tree_.Connect("somebody", "group");
                tree_.Connect("group", "everybody");
                tree_.Connect("attribute", "modifiedattribute");
                tree_.Connect("something", "event");
                tree_.Connect("event", "projectevent");
                tree_.Connect("event", "message");
                tree_.Connect("event", "transfer");
                tree_.Connect("transfer", "move");
                tree_.Connect("transfer", "trade");
                tree_.Connect("event", "objectchange");
                tree_.Connect("objectchange", "modification");
                tree_.Connect("objectchange", "insertion");
                tree_.Connect("objectchange", "deletion");
                tree_.Connect("event", "agreement");
                tree_.Connect("something", "configurationparameter");
                tree_.Connect("something", "configuration");
                tree_.Connect("configuration", "applicationconfiguration");
                tree_.Connect("configuration", "computerconfiguration");
                tree_.Connect("something", "digitalsource");
                tree_.Connect("something", "encoding");
                tree_.Connect("something", "digitalassetsource");
                tree_.Connect("digitalassetsource", "datafile");
                tree_.Connect("something", "artifact");
                tree_.Connect("artifact", "literarywork");
                tree_.Connect("artifact", "computersystem");
                tree_.Connect("artifact", "right");
                tree_.Connect("artifact", "monetary");
                tree_.Connect("monetary", "currency");
                tree_.Connect("something", "systemusergroup");
                tree_.Connect("something", "category");
                tree_.Connect("category", "personcategory");
                tree_.Connect("category", "companycategory");
                tree_.Connect("category", "vendiblecategory");
                tree_.Connect("something", "level");
                tree_.Connect("level", "categorylevel");
                tree_.Connect("categorylevel", "vendiblelevel");
                tree_.Connect("level", "addresslevel");
                tree_.Connect("something", "packagetemplate");
                tree_.Connect("something", "paymentterms");
                tree_.Connect("something", "transfercondition");
                tree_.Connect("something", "deliverymethod");
                tree_.Connect("something", "identifier");
                tree_.Connect("identifier", "barcode");
                tree_.Connect("something", "scheme");
                tree_.Connect("something", "societyobjectsworkspace");
                tree_.Connect("something", "societyobjectsring");
                tree_.Connect("something", "sequence");
                tree_.Connect("something", "country");
                tree_.Connect("something", "unitofmeasure");
                tree_.Connect("unitofmeasure", "temperatureunit");
                tree_.Connect("unitofmeasure", "lengthunit");
                tree_.Connect("lengthunit", "metriclengthunit");
                tree_.Connect("metriclengthunit", "millimetre");
                tree_.Connect("metriclengthunit", "metre");
                tree_.Connect("metriclengthunit", "kilometre");
                tree_.Connect("metriclengthunit", "decimetre");
                tree_.Connect("metriclengthunit", "centimetre");
                tree_.Connect("unitofmeasure", "volumeunit");
                tree_.Connect("volumeunit", "metricvolumeunit");
                tree_.Connect("metricvolumeunit", "millilitre");
                tree_.Connect("metricvolumeunit", "litre");
                tree_.Connect("metricvolumeunit", "decilitre");
                tree_.Connect("metricvolumeunit", "centilitre");
                tree_.Connect("unitofmeasure", "massunit");
                tree_.Connect("massunit", "metricmassunit");
                tree_.Connect("metricmassunit", "kilogram");
                tree_.Connect("metricmassunit", "hectogram");
                tree_.Connect("metricmassunit", "gram");
                tree_.Connect("unitofmeasure", "energymeasure");
                tree_.Connect("something", "physicalobject");
                tree_.Connect("something", "condition");
                tree_.Connect("something", "illustration");
                tree_.Connect("something", "word");
                tree_.Connect("something", "placementpackagetobemovedtoextension");
                tree_.Connect("something", "language");
                tree_.Connect("something", "eventinfo");
                tree_.Connect("something", "fixedtype");
            }
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
                            // We have another set of stepsiblings. Merge them into one list.
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
        static Response ClassHierarchyCallProxy(Request req, String className, String paramStr, Boolean isSoClass) {

            // Collecting all responses in the tree.
            List<Response> resps = CallAllHandlersInTypeHierarchy(req, className, paramStr, isSoClass);

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
        public static void Init(Boolean emulateDatabase) {

            PopulateSoTree(emulateDatabase);

            Response.ResponsesMergerRoutine_ = DefaultJsonMerger;

            String savedAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = null;

            // Registering proxy Society Objects handler.
            Handle.GET<Request, String, String>("/so/{?}/{?}", 
                (Request req, String className, String paramStr) => {
                    return ClassHierarchyCallProxy(req, className, paramStr, true); 
                }, 
                new HandlerOptions() {
                    ProxyDelegateTrigger = true,
                    TypeOfHandler = HandlerOptions.TypesOfHandler.OntologyMapping 
                }
            );

            // Registering proxy arbitrary database classes handler.
            Handle.GET<Request, String, String>("/db/{?}/{?}",
                (Request req, String className, String paramStr) => {
                    return ClassHierarchyCallProxy(req, className, paramStr, false);
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