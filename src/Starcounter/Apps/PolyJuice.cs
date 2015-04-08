using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Advanced.XSON;
using System.Text;

namespace PolyjuiceNamespace {

    public class Polyjuice {

        /// <summary>
        /// Set to true if database with SO should be emulated (unit tests).
        /// </summary>
        internal static Boolean EmulateSoDatabase = true;

        /// <summary>
        /// Global tree.
        /// </summary>
        static Tree tree_;

        /// <summary>
        /// Custom maps.
        /// </summary>
        static Dictionary<String, List<HandlerForSoType>> customMaps_;

        /// <summary>
        /// Supported parameter string.
        /// </summary>
        const String EndsWithStringParam = "@w";

        /// <summary>
        /// Suggested Polyjuice mapping URI.
        /// </summary>
        const String PolyjuiceMappingUri = "/polyjuice/";

        /// <summary>
        /// Handler information for mapped SO type.
        /// </summary>
        public class HandlerForSoType {

            /// <summary>
            /// Society Objects type.
            /// </summary>
            public SoType TheType;

            /// <summary>
            /// Handler ID.
            /// </summary>
            public UInt16 HandlerId;

            /// <summary>
            /// Conversion delegate to SO.
            /// </summary>
            public Func<String, String> ConverterToSo;

            /// <summary>
            /// Conversion delegate from SO.
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
        public class SoType {

            /// <summary>
            /// Parent in inheritance tree.
            /// </summary>
            public SoType Inherits;

            /// <summary>
            /// Name of the SO type.
            /// </summary>
            public String Name;

            /// <summary>
            /// List of handlers.
            /// </summary>
            public List<HandlerForSoType> Handlers;

            public SoType(string nm) {
                Handlers = new List<Polyjuice.HandlerForSoType>();
                Name = nm;
                Children = new HashSet<SoType>();
            }

            public HashSet<SoType> Children;

            public void AddChild(SoType n) {
                Children.Add(n);
            }
        }

        public class Tree {
            private Dictionary<string, SoType> nameToNodes;

            private SoType Locate(string name) {
                if (!nameToNodes.ContainsKey(name))
                    nameToNodes.Add(name, new SoType(name));
                return nameToNodes[name];
            }

            public void Connect(string nameFrom, string nameTo) {
                SoType nodeFrom = Locate(nameFrom);
                SoType nodeTo = Locate(nameTo);
                nodeTo.Inherits = nodeFrom;
                nodeFrom.AddChild(nodeTo);
            }

            public void Add(string name) {
                if (!nameToNodes.ContainsKey(name))
                    nameToNodes.Add(name, new SoType(name));
            }

            public SoType Find(string name) {
                if (nameToNodes.ContainsKey(name))
                    return nameToNodes[name];
                else return null;
            }

            public Tree() {
                nameToNodes = new Dictionary<string, SoType>();
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

        static public void ProduceLoader(SoType node, System.IO.StreamWriter file) {
            foreach (var v in node.Children) {
                file.WriteLine("tree_.Connect(\"" + node.Name + "\", \"" + v.Name + "\");");
                file.Flush();
                ProduceLoader(v, file);
            }
        }

        /// <summary>
        /// Gets type name from Society Objects URI.
        /// </summary>
        static String GetTypeNameFromSoUri(String soSubUri) {

            // Until first slash.
            Int32 slashOffset = 0;

            while (soSubUri[slashOffset] != '/') {
                slashOffset++;
            }

            // Skipping "/so/" in the beginning and "/@s" at the end.
            return soSubUri.Substring(0, slashOffset);
        }

        /// <summary>
        /// Maps an existing application processed URI to another URI.
        /// </summary>
        public static void Map(
            String appProcessedUri,
            String mapProcessedUri,
            String method = "GET") {

            Map(appProcessedUri, mapProcessedUri, null, null, method);
        }

        /// <summary>
        /// Mapping  handler that calls registered handlers.
        /// </summary>
        static Response MappingHandler(Request req, List<HandlerForSoType> mappedHandlersList, String stringParam) {

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
                foreach (HandlerForSoType x in mappedHandlersList) {

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
            foreach (HandlerForSoType x in mappedHandlersList) {

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
                return Response.ResponsesMergerRoutine_(req, null, resps);

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
            Func<String, String> converterTo,
            Func<String, String> converterFrom,
            String method) {

            // Checking if method is allowed.
            if (method != "GET" &&
                method != "PUT" &&
                method != "POST" &&
                method != "PATCH" &&
                method != "DELETE") {

                throw new InvalidOperationException("HTTP method should be either GET, POST, PUT, DELETE or PATCH.");
            }

            if (!StarcounterEnvironment.PolyjuiceAppsFlag) {
                throw new InvalidOperationException("Polyjuice is not initialized!");
            }

            lock (tree_) {

                // Checking that map URI is "/" or starts with "/polyjuice/".
                if (!mapProcessedUri.StartsWith(PolyjuiceMappingUri, StringComparison.InvariantCultureIgnoreCase)) {
                    if (mapProcessedUri != "/") {
                        throw new ArgumentException("Application can only map to handlers starting with \"/polyjuice/\" or a root handler.");
                    }
                }

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

                // Adding handler to SO type.
                HandlerForSoType handler = AddHandlerToSoType(
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
                    List<HandlerForSoType> mappedHandlersList = new List<HandlerForSoType>();
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
                            AllowNonPolyjuiceHandler = true,
                            ProxyDelegateTrigger = true
                        });

                    } else {

                        // Registering mapped URI.
                        Handle.CUSTOM(method + " " + mapProcessedUri, (Request req) => {
                            return MappingHandler(req, mappedHandlersList, null);
                        }, new HandlerOptions() {
                            AllowNonPolyjuiceHandler = true,
                            ProxyDelegateTrigger = true
                        });
                    }

                    StarcounterEnvironment.AppName = savedAppName;

                } else {

                    // Just adding this mapped handler to the existing list.

                    List<HandlerForSoType> mappedList;
                    Boolean found = customMaps_.TryGetValue(mapProcessedMethodUriSpace, out mappedList);
                    Debug.Assert(true == found);

                    mappedList.Add(handler);
                }

                // Checking if we have any parameters.
                if (numParams1 > 0) {

                    // Registering the SO handler as a map to corresponding application URI.
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
                        ProxyDelegateTrigger = true
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
                        ProxyDelegateTrigger = true
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

            if (!StarcounterEnvironment.PolyjuiceAppsFlag) {
                throw new InvalidOperationException("Polyjuice is not initialized!");
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

                // Getting string representation for the SO type.
                String typeName = GetTypeNameFromSoUri(soProcessedUri.Substring("/so/".Length));

                SoType soType = null;

                // Checking if we emulate the database with SO.
                if (EmulateSoDatabase) {

                    soType = tree_.Find(typeName);

                } else {
                    soType = (SoType)Db.SQL("SELECT T FROM SoType T WHERE Name = ?", typeName).First;
                }

                if (null == soType) {
                    throw new ArgumentException("Can not find Society Objects type in hierarchy: " + typeName);
                }

                // Adding handler to SO type.
                HandlerForSoType handler = AddHandlerToSoType(
                    soType,
                    handlerId,
                    appProcessedUri,
                    appProcessedMethodUriSpace,
                    handlerInfo.AppName,
                    converterToSo,
                    converterFromSo);

                // Registering the SO handler as a map to corresponding application URI.
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

                    Response resp = Self.GET("/so/" + typeName + "/" + soObjectId, null, req.HandlerOpts);

                    return resp;

                }, new HandlerOptions() {
                    ProxyDelegateTrigger = true
                });
            }
        }
        
        /// <summary>
        /// Calling all handlers in SO hierarchy.
        /// </summary>
        static List<Response> CallAllHandlersInTypeHierarchy(Request req, SoType soType, String paramStr) {

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

                Boolean currentAppHasHandler = false;

                SoType soTypeTemp = soType;

                // Until we reach the root of the SO tree.
                while ((soTypeTemp != null) && (false == currentAppHasHandler)) {

                    // Checking if application handler is presented in the hierarchy.
                    foreach (HandlerForSoType x in soTypeTemp.Handlers) {

                        if (x.AppName == req.HandlerOpts.CallingAppName) {

                            currentAppHasHandler = true;
                            break;
                        }
                    }

                    soTypeTemp = soTypeTemp.Inherits;
                }

                // Checking if application is not found.
                if (!currentAppHasHandler) {
                    return resps;
                }
            }

            // Until we reach the root of the SO tree.
            while (soType != null) {

                CallAllHandlersForSingleType(resps, soType, paramStr, alreadyCalledHandlers);
                soType = soType.Inherits;
            }

            return resps;
        }

        /// <summary>
        /// Adding existing handler to SO type tree.
        /// </summary>
        static HandlerForSoType AddHandlerToSoType(
            SoType soType,
            UInt16 handlerId,
            String handlerProcessedUri,
            String appProcessedMethodUriSpace,
            String appName,
            Func<String, String> converterToSo,
            Func<String, String> converterFromSo) {

            HandlerForSoType x = new HandlerForSoType();

            // Checking if we emulate the database with SO.
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

                // Adding handler id to the type.
                Db.Transact(() => {
                    x.TheType = soType;
                    x.HandlerId = handlerId;
                    x.HandlerProcessedUri = handlerProcessedUri;
                    x.ParamOffset = (UInt16)appProcessedMethodUriSpace.IndexOf('@');
                    x.AppName = appName;
                    x.ConverterToSo = converterToSo;
                    x.ConverterFromSo = converterFromSo;
                });
            }

            return x;
        }

        /// <summary>
        /// Calling all handlers associated with a given type.
        /// </summary>
        static void CallAllHandlersForSingleType(
            List<Response> resps,
            SoType type,
            String stringParam,
            List<UInt16> alreadyCalledHandlers) {

            // Processing specific handler.
            Action<HandlerForSoType> processHandler = (HandlerForSoType x) => {

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

            // Checking if we emulate the database with SO.
            if (EmulateSoDatabase) {

                foreach (HandlerForSoType x in type.Handlers) {

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
                }
            }
        }

        /// <summary>
        /// Populating SO tree.
        /// </summary>
        static void PopulateSoTree() {

            tree_ = new Tree();

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

        /// <summary>
        /// Default merger function for PolyJuice.
        /// </summary>
        public static Response DefaultMerger(Request req, Response resp, List<Response> responses) {
            Json siblingJson;
            Json mainJson;
            List<Json> stepSiblings;

            // Checking if there is only one response, which becomes the main response.
            
            if (resp != null) {

                mainJson = resp.Resource as Json;

                if (mainJson != null) {
                    mainJson._appName = resp.AppName;
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

                if (responses.Count == 1)
                    return mainResponse;

                stepSiblings = new List<Json>();
                mainJson.StepSiblings = stepSiblings;
                stepSiblings.Add(mainJson);

                for (Int32 i = 0; i < responses.Count; i++) {

                    if (mainResponseId != i) {
                        siblingJson = (Json)responses[i].Resource;

                        // TODO:
                        // Do we need to check the response in case of error and handle it or
                        // just ignore like we do now?

                        // No json in partial response. Probably because a registered handler didn't want to 
                        // add anything for this uri and data.
                        if (siblingJson == null) 
                            continue;

                        siblingJson._appName = responses[i].AppName;

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
            }

            return mainResponse;
        }

        /// <summary>
        /// Initializes everything needed for Polyjuice.
        /// </summary>
        public static void Init() {

            PopulateSoTree();

            customMaps_ = new Dictionary<string, List<HandlerForSoType>>();

            Response.ResponsesMergerRoutine_ = DefaultMerger;

            String savedAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = null;

            // Registering ubiquitous SocietyObjects handler.
            Handle.GET("/so/{?}/{?}", (Request req, String typeName, String paramStr) => {

                SoType soType = null;

                // Checking if we emulate the database with SO.
                if (EmulateSoDatabase) {

                    soType = tree_.Find(typeName);

                } else {

                    soType = (SoType)Db.SQL("SELECT T FROM SoType T WHERE Name = ?", typeName).First;
                }

                if (null == soType) {
                    throw new ArgumentException("Can not find Society Objects type in hierarchy: " + typeName);
                }

                // Collecting all responses in the tree.
                List<Response> resps = CallAllHandlersInTypeHierarchy(req, soType, paramStr);

                if (resps.Count > 0) {

                    // Creating merged response.
                    return Response.ResponsesMergerRoutine_(req, null, resps);

                } else {

                    // If there is no merged response - return null.
                    return null;
                }

            }, new HandlerOptions() {
                ProxyDelegateTrigger = true
            });

            // Merges HTML partials according to provided URLs.
            Handle.GET(StarcounterConstants.PolyjuiceHtmlMergerPrefix + "{?}", (String s) => {

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

            StarcounterEnvironment.AppName = savedAppName;

            // Now all applications are treated as Polyjuice applications.
            StarcounterEnvironment.PolyjuiceAppsFlag = true;
        }
    }
}