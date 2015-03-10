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

namespace Starcounter.Rest
{
    /// <summary>
    /// Information about registered URI.
    /// </summary>
    internal class RegisteredUriInfo
    {
        public String original_uri_info_ = null;
        public IntPtr original_uri_info_ascii_bytes_;
        public String processed_uri_info_ = null;
        public IntPtr processed_uri_info_ascii_bytes_;
        public Type param_message_type_ = null;
        public Func<object> param_message_create_ = null;
        public Byte[] native_param_types_ = null;
        public Byte num_params_ = 0;
        public UInt16 handler_id_ = HandlerOptions.InvalidUriHandlerId;
        public UInt64 handler_info_ = UInt64.MaxValue;
        public UInt16 port_ = 0;
        public MixedCodeConstants.NetworkProtocolType proto_type_ = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1;
        public MixedCodeConstants.HTTP_METHODS http_method_ = MixedCodeConstants.HTTP_METHODS.GET;

        public void Destroy()
        {
            original_uri_info_ = null;
            processed_uri_info_ = null;
            handler_id_ = HandlerOptions.InvalidUriHandlerId;

            if (original_uri_info_ascii_bytes_ != IntPtr.Zero)
            {
                // Releasing internal resources here.
                BitsAndBytes.Free(original_uri_info_ascii_bytes_);
                original_uri_info_ascii_bytes_ = IntPtr.Zero;
            }

            if (processed_uri_info_ascii_bytes_ != IntPtr.Zero)
            {
                // Releasing internal resources here.
                BitsAndBytes.Free(processed_uri_info_ascii_bytes_);
                processed_uri_info_ascii_bytes_ = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Initializes URI pointers.
        /// </summary>
        public void InitUriPointers()
        {
            unsafe
            {
                original_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(original_uri_info_.Length + 1);
                Byte[] temp = Encoding.ASCII.GetBytes(original_uri_info_);
                Byte* p = (Byte*) original_uri_info_ascii_bytes_.ToPointer();
                fixed (Byte* t = temp) {
                    BitsAndBytes.MemCpy(p, t, (uint)original_uri_info_.Length);
                }

                p[original_uri_info_.Length] = 0;

                processed_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(processed_uri_info_.Length + 1);
                temp = Encoding.ASCII.GetBytes(processed_uri_info_);
                p = (Byte*) processed_uri_info_ascii_bytes_.ToPointer();
                fixed (Byte* t = temp) {
                    BitsAndBytes.MemCpy(p, t, (uint)processed_uri_info_.Length);
                }

                p[processed_uri_info_.Length] = 0;
            }
        }

        /// <summary>
        /// Getting registered URI info for URI matcher creation.
        /// </summary>
        /// <returns></returns>
        public unsafe MixedCodeConstants.RegisteredUriManaged GetRegisteredUriManaged()
        {
            MixedCodeConstants.RegisteredUriManaged r = new MixedCodeConstants.RegisteredUriManaged();

            r.original_uri_info_string = original_uri_info_ascii_bytes_;
            r.processed_uri_info_string = processed_uri_info_ascii_bytes_;

            r.num_params = num_params_;

            // TODO: Resolve this hack with only positive handler ids in generated code.
            r.handler_id = handler_id_ + 1;

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
        /// Constructor.
        /// </summary>
        public UserHandlerInfo(UInt16 handlerId) {
            handlerId_ = handlerId;
        }

        /// <summary>
        /// Runs user/proxy delegate.
        /// </summary>
        public Response RunUserDelegate(
            Request req,
            IntPtr methodSpaceUriSpaceOnStack,
            IntPtr parametersInfoOnStack,
            HandlerOptions handlerOptions) {

            // Determining if proxy handler should be used.
            Boolean useProxyDelegate = (proxyDelegate_ != null) && (!handlerOptions.ProxyDelegateTrigger);

            Response resp = null;

            if (useProxyDelegate) {

                // Calling proxy user delegate.
                resp = proxyDelegate_(req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                // Checking if we have any response.
                if (null == resp) {

                    return null;

                }

            } else {

                Response subsResp = null;

                // Checking if there is a substitute handler.
                if (req.HandlerOpts.SubstituteHandler != null) {

                    // Calling substitute handler.
                    subsResp = req.HandlerOpts.SubstituteHandler();

                    // Checking if there is a substitute response.
                    if (subsResp == null)
                        return null;

                    // Setting the response application name.
                    subsResp.AppName = req.HandlerOpts.CallingAppName;

                    if (StarcounterEnvironment.PolyjuiceAppsFlag &&
                        (!String.IsNullOrEmpty(req.HandlerOpts.CallingAppName))) {

                        // Checking if we wanted to call the same application, then there is just substitution.
                        if (req.HandlerOpts.CallingAppName == appName_) {
                            return Response.ResponsesMergerRoutine_(req, subsResp, null);
                        }

                    } else {

                        return subsResp;
                    }
                } else {

                    // Checking that its not an outside call.
                    if (StarcounterEnvironment.PolyjuiceAppsFlag &&
                        (!handlerOptions.ProxyDelegateTrigger) &&
                        (!String.IsNullOrEmpty(req.HandlerOpts.CallingAppName)) &&
                        (req.HandlerOpts.CallingAppName != appName_)) {
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
                        if (StarcounterEnvironment.PolyjuiceAppsFlag) {
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
                    (StarcounterEnvironment.PolyjuiceAppsFlag)) {

                    // Checking if we have a substitute handler response.
                    if (subsResp != null) {
                        List<Response> respList = new List<Response>();
                        respList.Add(subsResp);
                        respList.Add(resp);
                        return Response.ResponsesMergerRoutine_(req, null, respList);
                    }

                    return Response.ResponsesMergerRoutine_(req, resp, null);
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

        public String OriginalUriInfo
        {
            get { return uri_info_.original_uri_info_; }
        }

        public String ProcessedUriInfo
        {
            get { return uri_info_.processed_uri_info_; }
        }

        public UInt16 Port
        {
            get { return uri_info_.port_; }
        }

        public bool IsEmpty()
        {
            return uri_info_.processed_uri_info_ == null;
        }

        public void Destroy()
        {
            uri_info_.Destroy();
            userDelegate_ = null;
        }

        public void TryAddProxyDelegate(
            Func<Request, IntPtr, IntPtr, Response> userDelegate,
            HandlerOptions ho)
        {
            // Checking if already have a proxy delegate.
            if (proxyDelegate_ != null) {
                throw new ArgumentOutOfRangeException("Can't add a proxy delegate to a handler that already contains a proxy delegate!");
            }

            // Checking if its a special delegate.
            if (ho.ProxyDelegateTrigger) {

                proxyDelegate_ = userDelegate;

            } else {

                throw new ArgumentException("Trying to add a delegate to an already existing handler!");
            }
        }

        public void Init(
            UInt16 port,
            String original_uri_info,
            String processed_uri_info,
            Func<Request, IntPtr, IntPtr, Response> user_delegate,
            Byte[] native_param_types,
            Type param_message_type,
            UInt16 handler_id,
            UInt64 handler_info,
            MixedCodeConstants.NetworkProtocolType protoType,
            HandlerOptions ho)
        {
            uri_info_.original_uri_info_ = original_uri_info;
            uri_info_.processed_uri_info_ = processed_uri_info;
            uri_info_.param_message_type_ = param_message_type;
            uri_info_.handler_id_ = handler_id;
            uri_info_.handler_info_ = handler_info;
            uri_info_.port_ = port;
            uri_info_.native_param_types_ = native_param_types;
            uri_info_.num_params_ = (Byte)native_param_types.Length;
            uri_info_.http_method_ = UriHelper.GetMethodFromString(original_uri_info);

            if (param_message_type != null)
                uri_info_.param_message_create_ = Expression.Lambda<Func<object>>(Expression.New(param_message_type)).Compile();

            Debug.Assert(userDelegate_ == null);

            if (ho.ProxyDelegateTrigger) {

                proxyDelegate_ = user_delegate;

            } else {

                userDelegate_ = user_delegate;
            }
            
            appName_ = StarcounterEnvironment.AppName;

            uri_info_.InitUriPointers();
        }
    }

    public class UriInjectMethods {

        internal static Func<IntPtr, IntPtr, Request, HandlerOptions, Response> runDelegateAndProcessResponse_;
        public static Func<Request, Boolean> OnHttpMessageRoot_;
        public delegate void RegisterUriHandlerNativeDelegate(
            UInt16 port,
            String appName,
            String originalUriInfo,
            String processedUriInfo,
            Byte[] nativeParamTypes,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo);

        internal static RegisterUriHandlerNativeDelegate RegisterUriHandlerNative_;

        // Checking if this Node supports local resting.
        internal static Boolean IsSupportingLocalNodeResting() {
            return runDelegateAndProcessResponse_ != null;
        }

        public static void SetDelegates(
            RegisterUriHandlerNativeDelegate registerUriHandlerNative,
            Func<Request, Boolean> onHttpMessageRoot,
            Func<IntPtr, IntPtr, Request, HandlerOptions, Response> runDelegateAndProcessResponse) {

            RegisterUriHandlerNative_ = registerUriHandlerNative;
            OnHttpMessageRoot_ = onHttpMessageRoot;
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

        public const Int32 MaxUriHandlers = 1024;

        internal void Reset()
        {
            maxNumHandlersEntries_ = 0;

            portUris_ = new List<PortUris>();

            allUriHandlers_ = new UserHandlerInfo[MaxUriHandlers];
            for (UInt16 i = 0; i < MaxUriHandlers; i++) {
                allUriHandlers_[i] = new UserHandlerInfo(i);
            }
        }

        public UriHandlersManager()
        {
            // Initializing port uris.
            PortUris.GlobalInit();

            allUriHandlers_ = new UserHandlerInfo[MaxUriHandlers];
            for (UInt16 i = 0; i < MaxUriHandlers; i++) {
                allUriHandlers_[i] = new UserHandlerInfo(i);
            }
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
            String originalUriInfo,
            String processedUriInfo,
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
                    if ((0 == String.Compare(allUriHandlers_[i].ProcessedUriInfo, processedUriInfo, true)) &&
                        (port == allUriHandlers_[i].Port))
                    {
                        allUriHandlers_[i].TryAddProxyDelegate(wrappedDelegate, ho);
                        return;
                    }
                }

                // NOTE: Always adding handler to the end of the list (so that handler IDs are always unique).
                handlerId = maxNumHandlersEntries_;
                maxNumHandlersEntries_++;

                UInt64 handlerInfo = UInt64.MaxValue;

                if (handlerId >= MaxUriHandlers) {
                    throw new ArgumentOutOfRangeException("Too many user handlers registered!");
                }

                allUriHandlers_[handlerId].Init(
                    port,
                    originalUriInfo,
                    processedUriInfo,
                    wrappedDelegate,
                    nativeParamTypes,
                    messageType,
                    handlerId,
                    handlerInfo,
                    protoType,
                    ho);

                // Registering the outer native handler (if any).
                if (UriInjectMethods.RegisterUriHandlerNative_ != null) {

                    // Checking if we are on default handler level so we register with gateway.
                    if (ho.HandlerLevel == HandlerOptions.HandlerLevels.DefaultLevel) {

                        String appName = allUriHandlers_[handlerId].AppName;
                        if (String.IsNullOrEmpty(appName)) {
                            appName = MixedCodeConstants.EmptyAppName;
                        }

                        UriInjectMethods.RegisterUriHandlerNative_(
                            port,
                            appName,
                            originalUriInfo,
                            processedUriInfo,
                            nativeParamTypes,
                            handlerId,
                            out handlerInfo);
                    }
                }

                // Updating handler info from received value.
                allUriHandlers_[handlerId].UriInfo.handler_info_ = handlerInfo;

                // Resetting port URIs matcher.
                PortUris portUris = SearchPort(port);
                if (portUris != null)
                    portUris.MatchUriAndGetHandlerId = null;
            }
        }

        /// <summary>
        /// Searches for existing processed URI handler.
        /// </summary>
        public UserHandlerInfo FindHandlerByProcessedUri(String processedUriInfo) {

            lock (allUriHandlers_) {

                for (UInt16 i = 0; i < MaxUriHandlers; i++) {

                    if (allUriHandlers_[i].ProcessedUriInfo == processedUriInfo) {
                        
                        return allUriHandlers_[i];
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Unregisters existing URI handler.
        /// </summary>
        /// <param name="methodAndUri"></param>
        public void UnregisterUriHandler(String methodAndUri)
        {
            lock (allUriHandlers_)
            {
                for (Int32 i = 0; i < MaxUriHandlers; i++)
                {
                    if (allUriHandlers_[i].ProcessedUriInfo == methodAndUri)
                    {
                        // TODO: Call underlying BMX handler destructor.

                        allUriHandlers_[i].Destroy();
                        maxNumHandlersEntries_--;

                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}