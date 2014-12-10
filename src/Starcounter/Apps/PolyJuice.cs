using Starcounter;
using Starcounter.Internal;
using Starcounter.Rest;
using System;
using System.Collections.Generic;

namespace PolyjuiceNamespace {

    public class Polyjuice {

        /// <summary>
        /// Set to true if database with SO should be emulated (unit tests).
        /// </summary>
        internal static Boolean EmulateSoDatabase = true;

        /// <summary>
        /// The list with all nodes.
        /// </summary>
        public static List<SoType> GlobalTypesList;

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

                foreach (SoType t in GlobalTypesList) {
                    if (t.Name == typeName) {
                        soType = t;
                        break;
                    }
                }
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

                Response resp;
                X.GET("/so/" + typeName + "/" + soObjectId, out resp);

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
        /// Initializes everything needed for Polyjuice.
        /// </summary>
        public static void Init() {

            HandlerOptions ho = new HandlerOptions();
            ho.DontMerge = true;

            // Registering ubiquitous SocietyObjects handler.
            Handle.GET("/so/{?}/{?}", (Request req, String typeName, String paramStr) => {

                SoType soType = null;

                // Checking if we emulate the database with SO.
                if (EmulateSoDatabase) {

                    foreach (SoType t in GlobalTypesList) {
                        if (t.Name == typeName) {
                            soType = t;
                            break;
                        }
                    }

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