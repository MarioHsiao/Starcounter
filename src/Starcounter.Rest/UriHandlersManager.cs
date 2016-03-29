using Starcounter;
using Starcounter.Internal;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;
using System.Web.UI.WebControls;
using Starcounter.Advanced.XSON;
using System.Runtime.InteropServices;

namespace Starcounter.Rest
{
    /// <summary>
    /// Information about registered URI.
    /// </summary>
    internal class RegisteredUriInfo
    {
        public String method_space_uri_ = null;
        public Type param_message_type_ = null;
        public Func<object> param_message_create_ = null;
        public Byte[] native_param_types_ = null;
        public Byte num_params_ = 0;
        public UInt16 handler_id_ = HandlerOptions.InvalidUriHandlerId;
        public UInt64 handler_info_ = UInt64.MaxValue;
        public UInt16 port_ = 0;
        public MixedCodeConstants.NetworkProtocolType proto_type_ = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1;
        public MixedCodeConstants.HTTP_METHODS http_method_ = MixedCodeConstants.HTTP_METHODS.GET;
        
        /// <summary>
        /// Checks if there is only one last parameter of type string.
        /// </summary>
        /// <returns></returns>
        public Boolean HasOneLastParamOfTypeString() {
            return (num_params_ == 1) && (MixedCodeConstants.REST_ARG_STRING == native_param_types_[0]);
        }

        public void Destroy()
        {
            method_space_uri_ = null;
            handler_id_ = HandlerOptions.InvalidUriHandlerId;
        }

        /// <summary>
        /// Getting registered URI info for URI matcher creation.
        /// </summary>
        /// <returns></returns>
        public unsafe MixedCodeConstants.RegisteredUriManaged GetRegisteredUriManaged()
        {
            MixedCodeConstants.RegisteredUriManaged r = new MixedCodeConstants.RegisteredUriManaged();

            r.method_space_uri = Marshal.StringToHGlobalAnsi(method_space_uri_);
            r.num_params = num_params_;

            r.handler_id = handler_id_;

            for (Int32 i = 0; i < native_param_types_.Length; i++) {
                r.param_types[i] = native_param_types_[i];
            }

            return r;
        }
    }

    /// <summary>
    /// URI handler information.
    /// </summary>
    internal class UserHandlerInfo
    {
        /// <summary>
        /// Handler ID.
        /// </summary>
        readonly UInt16 handlerId_;

        /// <summary>
        /// User delegate.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> userDelegate_ = null;

        /// <summary>
        /// Proxy delegate.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> proxyDelegate_ = null;

        /// <summary>
        /// Saved proxy delegate.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> savedProxyDelegate_ = null;

        /// <summary>
        /// Owner application name.
        /// </summary>
        String appName_ = null;

        /// <summary>
        /// Handler ID.
        /// </summary>
        public UInt16 HandlerId {
            get {
                return handlerId_;
            }
        }

        /// <summary>  
        /// Type of handler.
        /// </summary>
        HandlerOptions.TypesOfHandler typeOfHandler_;

        /// <summary>
        /// Try if request filters should be skipped.
        /// </summary>
        internal Boolean SkipRequestFilters {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserHandlerInfo(UInt16 handlerId) {
            handlerId_ = handlerId;
        }

        /// <summary>
        /// Enables/Disables mapping.
        /// </summary>
        public void EnableDisableMapping(Boolean enable, HandlerOptions.TypesOfHandler typeOfHandler) {

            if (typeOfHandler_ == typeOfHandler) {

                if (enable) {
                    proxyDelegate_ = savedProxyDelegate_;
                } else {
                    savedProxyDelegate_ = proxyDelegate_;
                    proxyDelegate_ = null;
                }
            }
        }

        /// <summary>
        /// Runs user/proxy delegate.
        /// </summary>
        public Response RunUserDelegate(
            Request req,
            IntPtr methodSpaceUriSpaceOnStack,
            IntPtr parametersInfoOnStack) {

            HandlerOptions handlerOptions = req.HandlerOpts;

            // Determining if proxy handler should be used.
            Boolean useProxyDelegate = (proxyDelegate_ != null) && (!handlerOptions.ProxyDelegateTrigger);

            Response resp = null;

            if (useProxyDelegate) {

                // Calling proxy user delegate.
                resp = proxyDelegate_(req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                // Checking if we have any response.
                if (null == resp)
                    return null;

            } else {

                // Checking if delegate is defined.
                if (userDelegate_ == null)
                    return null;

                // Increasing calling level for internal calls.
                Int32 savedCallLevel = Handle.CallLevel;
                Handle.CallLevel++;

                try {

                    Response subsResp = null;

                    // Checking if there is a substitute handler.
                    if (handlerOptions.SubstituteHandler != null) {

                        // Calling substitute handler.
                        subsResp = handlerOptions.SubstituteHandler();

                        // Checking if there is a substitute response.
                        if (subsResp == null)
                            return null;

                        // Setting the response application name.
                        subsResp.AppName = handlerOptions.CallingAppName;

                        if (StarcounterEnvironment.MergeJsonSiblings &&
                            (!String.IsNullOrEmpty(handlerOptions.CallingAppName))) {

                            // Checking if we wanted to call the same application, then there is just substitution.
                            if (handlerOptions.CallingAppName == appName_) {
                                return Response.ResponsesMergerRoutine_(req, subsResp, null);
                            }
                        } else {

                            return subsResp;
                        }
                    } else {

                        // Checking that its not an outside call.
                        if (StarcounterEnvironment.MergeJsonSiblings &&
                            (!handlerOptions.ProxyDelegateTrigger) &&
                            (!String.IsNullOrEmpty(handlerOptions.CallingAppName)) &&
                            (handlerOptions.CallingAppName != appName_)) {

                            return null;
                        }
                    }

                    // Setting current application name.
                    StarcounterEnvironment.AppName = appName_;

                    // Calling intermediate user delegate.
                    resp = userDelegate_(req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                    // Checking if we have any response.
                    if (null == resp) {

                        if (subsResp != null) {
                            if (StarcounterEnvironment.MergeJsonSiblings) {
                                return Response.ResponsesMergerRoutine_(req, subsResp, null);
                            }
                            return subsResp;
                        }

                        return null;

                    } else {

                        // Setting to which application the response belongs.
                        resp.AppName = appName_;
                    }

                    // Checking if we need to merge.
                    if ((!handlerOptions.ProxyDelegateTrigger) &&
                        (StarcounterEnvironment.MergeJsonSiblings)) {

                        // Checking if we have a substitute handler response.
                        if (subsResp != null) {
                            List<Response> respList = new List<Response>();
                            respList.Add(subsResp);
                            respList.Add(resp);
                            return Response.ResponsesMergerRoutine_(req, null, respList);
                        }

                        return Response.ResponsesMergerRoutine_(req, resp, null);
                    }

                } finally {

                    // Restoring calling level for internal calls.
                    Handle.CallLevel = savedCallLevel;
                }
            }

            return resp;
        }

        RegisteredUriInfo uri_info_ = new RegisteredUriInfo();

        public RegisteredUriInfo UriInfo
        {
            get { return uri_info_; }
        }

        public Type ArgMessageType
        {
            get { return uri_info_.param_message_type_; }
            set { uri_info_.param_message_type_ = value; }
        }

        public Func<object> ArgMessageCreate {
            get { return uri_info_.param_message_create_; }
        }

        public String AppName
        {
            get {
                return appName_;
            }
        }

        public String MethodSpaceUri
        {
            get { return uri_info_.method_space_uri_; }
        }

        public UInt16 Port
        {
            get { return uri_info_.port_; }
        }

        public bool IsEmpty()
        {
            return uri_info_.method_space_uri_ == null;
        }

        public void Destroy()
        {
            uri_info_.Destroy();
            userDelegate_ = null;
        }

        public void TryAddProxyOrReplaceDelegate(
            Func<Request, IntPtr, IntPtr, Response> userDelegate,
            HandlerOptions ho)
        {
            // Checking if we are replacing the delegate.
            if (ho.ReplaceExistingHandler) {

                // Checking if its a proxy trigger we are trying to replace.
                if (ho.ProxyDelegateTrigger) {

                    proxyDelegate_ = userDelegate;

                } else {

                    userDelegate_ = userDelegate;
                }

                typeOfHandler_ = ho.TypeOfHandler;

            } else {

                // Checking if already have a proxy delegate.
                if (ho.ProxyDelegateTrigger) {

                    if (proxyDelegate_ != null) {

                        throw new ArgumentOutOfRangeException("Can't add a proxy delegate to a handler that already has a proxy delegate: " + 
                            MethodSpaceUri + " on port " + Port);

                    } else {

                        proxyDelegate_ = userDelegate;
                        typeOfHandler_ = ho.TypeOfHandler;
                    }

                } else {

                    throw ErrorCode.ToException(Error.SCERRHANDLERALREADYREGISTERED, MethodSpaceUri + " on port " + Port);
                }
            }
        }

        public void Init(
            UInt16 port,
            String method_space_uri,
            Func<Request, IntPtr, IntPtr, Response> user_delegate,
            Byte[] native_param_types,
            Type param_message_type,
            UInt16 handler_id,
            UInt64 handler_info,
            MixedCodeConstants.NetworkProtocolType protoType,
            HandlerOptions ho)
        {
            uri_info_.method_space_uri_ = method_space_uri;
            uri_info_.param_message_type_ = param_message_type;
            uri_info_.handler_id_ = handler_id;
            uri_info_.handler_info_ = handler_info;
            uri_info_.port_ = port;
            uri_info_.native_param_types_ = native_param_types;
            uri_info_.num_params_ = (Byte)native_param_types.Length;
            uri_info_.http_method_ = UriHelper.GetMethodFromString(method_space_uri);

            if (param_message_type != null) {
                uri_info_.param_message_create_ = Expression.Lambda<Func<object>>(Expression.New(param_message_type)).Compile();
            }

            Debug.Assert(userDelegate_ == null);

            SkipRequestFilters = ho.SkipRequestFilters || StarcounterEnvironment.SkipRequestFiltersGlobal;

            if (ho.ProxyDelegateTrigger) {

                proxyDelegate_ = user_delegate;

            } else {

                userDelegate_ = user_delegate;
            }

            typeOfHandler_ = ho.TypeOfHandler;
            
            appName_ = StarcounterEnvironment.AppName;
        }
    }

    public class UriInjectMethods {

        internal static Func<IntPtr, IntPtr, Request, Response> runDelegateAndProcessResponse_;
        public static Func<Request, Boolean> processExternalRequest_;
        public delegate void RegisterHttpHandlerInGatewayDelegate(
            UInt16 port,
            String appName,
            String methodSpaceUri,
            Byte[] nativeParamTypes,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo);

        public delegate void UnregisterHttpHandlerInGatewayDelegate(
            UInt16 port,
            String methodSpaceUri);

        internal static RegisterHttpHandlerInGatewayDelegate registerHttpHandlerInGateway_;

        internal static UnregisterHttpHandlerInGatewayDelegate unregisterHttpHandlerInGateway_;

        // Checking if this Node supports local resting.
        internal static Boolean IsSupportingLocalNodeResting() {
            return runDelegateAndProcessResponse_ != null;
        }

        public static void SetDelegates(
            RegisterHttpHandlerInGatewayDelegate registerHttpHandlerInGateway,
            UnregisterHttpHandlerInGatewayDelegate unregisterHttpHandlerInGateway,
            Func<Request, Boolean> processExternalRequest,
            Func<IntPtr, IntPtr, Request, Response> runDelegateAndProcessResponse) {

            registerHttpHandlerInGateway_ = registerHttpHandlerInGateway;
            unregisterHttpHandlerInGateway_ = unregisterHttpHandlerInGateway;
            processExternalRequest_ = processExternalRequest;
            runDelegateAndProcessResponse_ = runDelegateAndProcessResponse;
        }

        /// <summary>
        /// Incorrect session exception.
        /// </summary>
        public class IncorrectSessionException : Exception { }
    }

    /// <summary>
    /// All registered handlers manager.
    /// </summary>
    internal class UriHandlersManager
    {
        UserHandlerInfo[] allUriHandlers_ = null;

        List<PortUris> portUris_ = new List<PortUris>();

        UInt16 maxNumHandlersEntries_ = 0;

        public List<PortUris> PortUrisList
        {
            get { return portUris_; }
        }

        public PortUris SearchPort(UInt16 port)
        {
            foreach (PortUris pu in PortUrisList)
            {
                if (pu.Port == port)
                    return pu;
            }
            return null;
        }

        public PortUris AddPort(UInt16 port)
        {
            lock (UriInjectMethods.runDelegateAndProcessResponse_) {

                // Searching for existing port if any.
                PortUris portUris = SearchPort(port);
                if (portUris != null)
                    return portUris;

                // Adding new port entry.
                portUris = new PortUris(port);
                portUris_.Add(portUris);
                return portUris;
            }
        }

        /// <summary>
        /// Creating all needed handler levels.
        /// </summary>
        static UriHandlersManager[] uriHandlersManagers_ = new UriHandlersManager[Enum.GetNames(typeof(HandlerOptions.HandlerLevels)).Length];

        /// <summary>
        /// Initializes URI handler manager.
        /// </summary>
        internal static void Init() {

            for (Int32 i = 0; i < uriHandlersManagers_.Length; i++) {
                uriHandlersManagers_[i] = new UriHandlersManager();
            }
        }

        public static UriHandlersManager GetUriHandlersManager(HandlerOptions.HandlerLevels handlerLevel) {

            return uriHandlersManagers_[(Int32) handlerLevel];
        }

        internal static void ResetUriHandlersManagers() {

            Init();

            for (Int32 i = 0; i < uriHandlersManagers_.Length; i++) {
                uriHandlersManagers_[i].Reset();
            }
        }

        public UserHandlerInfo[] AllUserHandlerInfos
        {
            get { return allUriHandlers_; }
        }

        public Int32 NumRegisteredHandlers
        {
            get { return maxNumHandlersEntries_; }
        }

        internal void Reset()
        {
            maxNumHandlersEntries_ = 0;

            portUris_ = new List<PortUris>();

            allUriHandlers_ = new UserHandlerInfo[MixedCodeConstants.MAX_TOTAL_NUMBER_OF_HANDLERS];
            for (UInt16 i = 0; i < allUriHandlers_.Length; i++) {
                allUriHandlers_[i] = new UserHandlerInfo(i);
            }
        }

        public UriHandlersManager()
        {
            // Initializing port uris.
            PortUris.GlobalInit();

            Reset();
        }

        public static UInt16 GetPortNumber(Request req, HandlerOptions handlerOptions) {

            UriHandlersManager uhm = UriHandlersManager.GetUriHandlersManager(handlerOptions.HandlerLevel);

            return uhm.AllUserHandlerInfos[req.ManagedHandlerId].UriInfo.port_;
        }

        /// <summary>
        /// Registers URI handler on a specific port.
        /// </summary>
        public void RegisterUriHandler(
            UInt16 port,
            String methodSpaceUri,
            Byte[] nativeParamTypes,
            Type messageType,
            Func<Request, IntPtr, IntPtr, Response> wrappedDelegate,
            MixedCodeConstants.NetworkProtocolType protoType,
            HandlerOptions ho)
        {
            lock (allUriHandlers_)
            {
                UInt16 handlerId = 0;

                // Checking if URI already registered.
                for (Int32 i = 0; i < maxNumHandlersEntries_; i++)
                {
                    if ((0 == String.Compare(allUriHandlers_[i].MethodSpaceUri, methodSpaceUri, true)) &&
                        (port == allUriHandlers_[i].Port))
                    {
                        allUriHandlers_[i].TryAddProxyOrReplaceDelegate(wrappedDelegate, ho);
                        return;
                    }
                }

                // NOTE: Always adding handler to the end of the list (so that handler IDs are always unique).
                handlerId = maxNumHandlersEntries_;
                maxNumHandlersEntries_++;

                UInt64 handlerInfo = UInt64.MaxValue;

                if (handlerId >= allUriHandlers_.Length) {
                    throw ErrorCode.ToException(Error.SCERRMAXHANDLERSREACHED);
                }

                allUriHandlers_[handlerId].Init(
                    port,
                    methodSpaceUri,
                    wrappedDelegate,
                    nativeParamTypes,
                    messageType,
                    handlerId,
                    handlerInfo,
                    protoType,
                    ho);

                // Registering the outer native handler (if any).
                if (UriInjectMethods.registerHttpHandlerInGateway_ != null) {

                    // Checking if we are on default handler level so we register with gateway.
                    if ((ho.HandlerLevel == HandlerOptions.HandlerLevels.DefaultLevel) && (!ho.SelfOnly)) {

                        String appName = allUriHandlers_[handlerId].AppName;
                        if (String.IsNullOrEmpty(appName)) {
                            appName = MixedCodeConstants.EmptyAppName;
                        }

                        UriInjectMethods.registerHttpHandlerInGateway_(
                            port,
                            appName,
                            methodSpaceUri,
                            nativeParamTypes,
                            handlerId,
                            out handlerInfo);
                    }
                }

                // Updating handler info from received value.
                allUriHandlers_[handlerId].UriInfo.handler_info_ = handlerInfo;

                // Resetting port URIs matcher.
                PortUris portUris = SearchPort(port);
                if (portUris != null) {
                    portUris.matchUriAndGetHandlerIdFunc_ = null;
                }
            }
        }

        /// <summary>
        /// Searches for existing URI handler.
        /// </summary>
        public UserHandlerInfo FindHandlerByUri(String methodSpaceUri) {

            lock (allUriHandlers_) {

                for (UInt16 i = 0; i < maxNumHandlersEntries_; i++) {

                    if (0 == String.Compare(allUriHandlers_[i].MethodSpaceUri, methodSpaceUri, true)) {

                        return allUriHandlers_[i];
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Enables/Disables mapping handlers.
        /// </summary>
        public void EnableDisableMapping(Boolean enable, HandlerOptions.TypesOfHandler typeOfHandler) {

            lock (allUriHandlers_) {

                for (UInt16 i = 0; i < maxNumHandlersEntries_; i++) {

                    allUriHandlers_[i].EnableDisableMapping(enable, typeOfHandler);
                }
            }
        }

        /// <summary>
        /// Unregisters existing HTTP handler.
        /// </summary>
        public void UnregisterHttpHandler(UInt16 port, String methodSpaceUri)
        {
            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    port = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    port = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            lock (allUriHandlers_)
            {
                for (Int32 i = 0; i < allUriHandlers_.Length; i++)
                {
                    if (allUriHandlers_[i].MethodSpaceUri == methodSpaceUri)
                    {
                        // Destroying handler.
                        allUriHandlers_[i].Destroy();

                        // Unregistering handler in gateway.
                        UriInjectMethods.unregisterHttpHandlerInGateway_(
                            port,
                            methodSpaceUri);

                        // Resetting port HTTP matcher.
                        PortUris portUris = SearchPort(port);
                        if (portUris != null) {
                            portUris.matchUriAndGetHandlerIdFunc_ = null;
                        }
                    }
                }
            }
        }
    }
}