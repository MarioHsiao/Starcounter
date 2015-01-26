﻿using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using Starcounter.Advanced.XSON;

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

            while(soSubUri[slashOffset] != '/') {
                slashOffset++;
            }

            // Skipping "/so/" in the beginning and "/@s" at the end.
            return soSubUri.Substring(0, slashOffset);
        }

        /// <summary>
        /// Maps an existing application processed URI to Society Objects URI.
        /// </summary>
        public static void Map(
            String appProcessedUri,
            String soProcessedUri,
            Func<String, String> converterToSo,
            Func<String, String> converterFromSo) {

            // Checking that we have only long parameter.
            Int32 numParams1 = appProcessedUri.Split('@').Length - 1,
                numParams2 = soProcessedUri.Split('@').Length - 1;

            if ((numParams1 != 1) || (numParams2 != 1)) {
                throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type long.");
            }

            numParams1 = appProcessedUri.Split(new String[] { "@w" }, StringSplitOptions.None).Length - 1;
            numParams2 = soProcessedUri.Split(new String[] { "@w" }, StringSplitOptions.None).Length - 1;

            if ((numParams1 != 1) || (numParams2 != 1)) {
                throw new ArgumentException("Right now mapping is only allowed for URIs with one parameter of type long.");
            }

            // There is always a space at the end of processed URI.
            String appProcessedMethodUriSpace = "GET " + appProcessedUri.ToLowerInvariant() + " ";

            // Searching the handler by processed URI.
            UserHandlerInfo handlerInfo = UriManagedHandlersCodegen.FindHandlerByProcessedUri(appProcessedMethodUriSpace, HandlerOptions.DefaultHandlerOptions);

            // Checking how many Apps have registered the same URI.
            if (handlerInfo.AppNamesList.Count > 1) {
                throw new ArgumentException("More than one App registered the same handler: " + appProcessedUri);
            }

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
                soType = (SoType) Db.SQL("SELECT T FROM SoType T WHERE Name = ?", typeName).First;
            }

            if (null == soType) {
                throw new ArgumentException("Can not find Society Objects type in hierarchy: " + typeName);
            }

            // Checking that App has not registered any mappers on the same path.
            if (SameApplicationInTypeHierarchy(soType, handlerInfo.AppNamesList[0])) {
                throw new ArgumentException("Application has already registered a mapping handler on the same SO tree path!");
            }

            // Adding handler to SO type.
            HandlerForSoType handler = AddHandlerToSoType(
                soType,
                handlerId,
                appProcessedUri,
                appProcessedMethodUriSpace,
                handlerInfo.AppNamesList[0],
                converterToSo,
                converterFromSo);

            // Registering the SO handler as a map to corresponding application URI.
            Handle.GET(appProcessedUri.Replace("@w", "{?}"), (Request req, String appObjectId) => {

                String soObjectId = appObjectId;

                // Calling the conversion delegate.
                if (handler.ConverterToSo != null) {
                    soObjectId = handler.ConverterToSo(appObjectId);
                }

                // Setting calling application name.
                HandlerOptions ho = new HandlerOptions() {
                    AppName = StarcounterEnvironment.AppName
                };

                Response resp;
                X.GET("/so/" + typeName + "/" + soObjectId, out resp, null, 0, ho);

                return resp;

            }, new HandlerOptions() {
                ProxyDelegateTrigger = true
            });
        }

        /// <summary>
        /// Checking same application mapping inside same path in the tree.
        /// </summary>
        static Boolean SameApplicationInTypeHierarchy(SoType soType, String currentAppName) {

            // Until we reach the root of the SO tree.
            while (soType != null) {

                // Checking if we emulate the database with SO.
                if (EmulateSoDatabase) {

                    foreach (HandlerForSoType x in soType.Handlers) {

                        // Checking if this application handler is already presented in path.
                        if (x.AppName == currentAppName) {

                            return true;
                        }
                    }

                } else {

                    foreach (HandlerForSoType x in Db.SQL("SELECT X FROM HandlerForSoType X WHERE X.TheType = ?", soType)) {

                        // Checking if this application handler is already presented in path.
                        if (x.AppName == currentAppName) {

                            return true;
                        }
                    }
                }

                soType = soType.Inherits;
            }

            return false;
        }

        /// <summary>
        /// Calling all handlers in SO hierarchy.
        /// </summary>
        static List<Response> CallAllHandlersInTypeHierarchy(Request req, SoType soType, String paramStr) {

            List<Response> resps = new List<Response>();

            // Until we reach the root of the SO tree.
            while (soType != null) {

                CallAllHandlersForSingleType(resps, req, soType, paramStr);
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
                x.ParamOffset = (UInt16) appProcessedMethodUriSpace.IndexOf('@');
                x.AppName = appName;
                x.ConverterToSo = converterToSo;
                x.ConverterFromSo = converterFromSo;

                soType.Handlers.Add(x);

            } else {

                // Adding handler id to the type.
                Db.Transaction(() => {
                    x.TheType = soType;
                    x.HandlerId = handlerId;
                    x.HandlerProcessedUri = handlerProcessedUri;
                    x.ParamOffset = (UInt16) appProcessedMethodUriSpace.IndexOf('@');
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
        static void CallAllHandlersForSingleType(List<Response> resps, Request req, SoType type, String paramStr) {
        
            HandlerOptions ho = new HandlerOptions();
            ho.ProxyDelegateTrigger = true;

            // Processing specific handler.
            Action<HandlerForSoType> processHandler = (HandlerForSoType x) => {

                // Calling the conversion delegate.
                if (x.ConverterFromSo != null) {
                    paramStr = x.ConverterFromSo(paramStr);
                }

                // Setting handler level.
                ho.HandlerId = x.HandlerId;

                // Setting parameters info.
                MixedCodeConstants.UserDelegateParamInfo paramInfo;
                paramInfo.offset_ = x.ParamOffset;
                paramInfo.len_ = (UInt16)paramStr.Length;

                ho.ParametersInfo = paramInfo;

                // Setting calling string.
                String uri = x.HandlerProcessedUri.Replace("@w", paramStr);

                // Calling handler.
                Response resp;
                X.GET(uri, out resp, null, 0, ho);
                resps.Add(resp);

            };

            // Checking if we emulate the database with SO.
            if (EmulateSoDatabase) {
                foreach (HandlerForSoType x in type.Handlers) {
                    processHandler(x);
                }
            } else {
                foreach (HandlerForSoType x in Db.SQL("SELECT X FROM HandlerForType X WHERE X.TheType = ?", type)) {
                    processHandler(x);
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
        public static Response DefaultMerger(Request req, List<Response> responses) {

                var mainResponse = responses[0];
                Int32 mainResponseId = 0;

                // Searching for the current application in responses.
                for (Int32 i = 0; i < responses.Count; i++) {

                    if (responses[i].AppName == StarcounterEnvironment.AppName) {

                        mainResponse = responses[i];
                        mainResponseId = i;
                        break;
                    }
                }

                Json mainJson = mainResponse.Resource as Json;

                if ((mainJson != null) && (mainResponse.AppName != StarcounterConstants.LauncherAppName)) {

                    mainJson.SetAppName(mainResponse.AppName);

                    for (Int32 i = 0; i < responses.Count; i++) {

                        if (mainResponseId != i) {

                            ((Json)responses[i].Resource).SetAppName(responses[i].AppName);
                            mainJson.AddStepSibling((Json)responses[i].Resource);
                        }
                    }
                }

                return mainResponse;
            }

        /// <summary>
        /// Initializes everything needed for Polyjuice.
        /// </summary>
        public static void Init() {

            HandlerOptions ho = new HandlerOptions();
            ho.DontMerge = true;

            PopulateSoTree();

            // Registering ubiquitous SocietyObjects handler.
            Handle.GET("/so/{?}/{?}", (Request req, String typeName, String paramStr) => {

                SoType soType = null;

                // Checking if we emulate the database with SO.
                if (EmulateSoDatabase) {

                    soType = tree_.Find(typeName);

                } else {

                    soType = (SoType) Db.SQL("SELECT T FROM SoType T WHERE Name = ?", typeName).First;
                }

                if (null == soType) {
                    throw new ArgumentException("Can not find Society Objects type in hierarchy: " + typeName);
                }

                // Collecting all responses in the tree.
                List<Response> resps = CallAllHandlersInTypeHierarchy(req, soType, paramStr);

                // Creating merged response.
                if (resps.Count > 0) {
                    return UriInjectMethods.ResponsesMergerRoutine_(req, resps);
                } else {
                    // If there are no responses - return an empty JSON.
                    return new Json();
                }

            }, ho);
        }
    }
}