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
        /// List of user delegates.
        /// </summary>
        List<Func<Request, IntPtr, IntPtr, Response>> userDelegates_ = null;

        /// <summary>
        /// Proxy delegate.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> proxyDelegate_ = null;

        /// <summary>
        /// List of application names.
        /// </summary>
        List<String> appNames_ = null;

        /// <summary>
        /// Don't merge on this handler.
        /// </summary>
        Boolean dontMerge_ = false;

        /// <summary>
        /// Handler ID.
        /// </summary>
        public UInt16 HandlerId {
            get {
                return handlerId_;
            }
        }

        /// <summary>
        /// Is proxy delegate?
        /// </summary>
        public Boolean IsProxyDelegate {
            get {
                return (proxyDelegate_ != null);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserHandlerInfo(UInt16 handlerId) {
            handlerId_ = handlerId;
        }

        /// <summary>
        /// Start the session that came with request.
        /// </summary>
        void StartSessionThatCameWithRequest(Request req) {

            // Checking if we are in session already.
            if (req.IsExternal && req.CameWithCorrectSession) {

                // Obtaining session.
                Session s = (Session) req.GetAppsSessionInterface();

                // Checking if correct session was obtained.
                if (null != s) {

                    // Starting session.
                    Session.Start(s);
                }
            }
        }

        /// <summary>
        /// Runs all user delegates.
        /// </summary>
        public Response RunUserDelegates(
            Request req,
            IntPtr methodSpaceUriSpaceOnStack,
            IntPtr parametersInfoOnStack,
            HandlerOptions handlerOptions) {

            List<Response> responses;

            Debug.Assert(userDelegates_ != null);

            // Determining if proxy handler should be used.
            Boolean useProxyHandler = (proxyDelegate_ != null) && (!handlerOptions.ProxyDelegateTrigger);

            // Don't merge handler.
            Boolean dontMerge = dontMerge_ || handlerOptions.DontMerge;

            // Starting the session that came with request.
            StartSessionThatCameWithRequest(req);

            // Checking if there is only one delegate or merge function is not defined.
            if (userDelegates_.Count == 1) {

                Response resp = null;

                if (useProxyHandler) {

                    // Setting current application name.
                    if (handlerOptions.AppName != null) {
                        StarcounterEnvironment.AppName = handlerOptions.AppName;
                    }

                    // Calling proxy user delegate.
                    resp = proxyDelegate_(req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                    // Checking if we have any response.
                    if (null == resp) {

                        return null;

                    } else {

                        // Setting to which application the response belongs.
                        Debug.Assert(null != resp.AppName);
                    }

                } else {

                    // Setting current application name.
                    StarcounterEnvironment.AppName = appNames_[0];

                    if (handlerOptions.AppName != null) {
                        StarcounterEnvironment.AppName = handlerOptions.AppName;
                    }

                    // Calling intermediate user delegate.
                    resp = userDelegates_[0](req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                    // Checking if we have any response.
                    if (null == resp) {

                        return null;

                    } else {

                        // Setting to which application the response belongs.
                        resp.AppName = appNames_[0];
                    }

                    // Checking if we need to merge.
                    if ((UriInjectMethods.ResponsesMergerRoutine_ != null) && (!dontMerge)) {

                        responses = new List<Response>();
                        responses.Add(resp);
                        return UriInjectMethods.ResponsesMergerRoutine_(req, responses);
                    }
                }

                return resp;
            }

            Debug.Assert(false == useProxyHandler);

            responses = new List<Response>();

            // Running every delegate from the list.
            for (Int32 i = 0; i < userDelegates_.Count; i++) {

                var func = userDelegates_[i];

                // Setting application name.
                StarcounterEnvironment.AppName = appNames_[i];

                // Calling intermediate user delegate.
                Response resp = func(req, methodSpaceUriSpaceOnStack, parametersInfoOnStack);

                // Checking if we have any response.
                if (null == resp) {
                    continue;
                }

                // Setting to which application the response belongs.
                resp.AppName = appNames_[i];

                // Adding responses to the list.
                responses.Add(resp);
            }

            // Checking if we have a response merging function defined.
            Debug.Assert(UriInjectMethods.ResponsesMergerRoutine_ != null);

            // Creating merged response.
            return UriInjectMethods.ResponsesMergerRoutine_(req, responses);
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

        public List<String> AppNamesList {
            get {
                return appNames_;
            }
        }

        public String AppNames
        {
            get {
                String combinedAppNames = "";
                foreach (String a in appNames_)
                    combinedAppNames += a + "-";

                return combinedAppNames.TrimEnd(new Char[] { '-' });
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
            userDelegates_ = null;
        }

        public void AddDelegateToList(
            Func<Request, IntPtr, IntPtr, Response> userDelegate,
            HandlerOptions ho)
        {
            if (proxyDelegate_ != null) {
                throw new ArgumentOutOfRangeException("Can't add delegate to a handler that already contains a proxy delegate!");
            }

            // Checking if its a special delegate.
            if (ho.ProxyDelegateTrigger) {

                if (userDelegates_.Count > 1) {
                    throw new ArgumentOutOfRangeException("Can't add a proxy delegate. Handler already contains more than one delegate!");
                }

                proxyDelegate_ = userDelegate;

            } else {

                userDelegates_.Add(userDelegate);

                // Checking if application is already on the list.
                foreach (String a in appNames_) {
                    if (a == StarcounterEnvironment.AppName) {
                        throw new ArgumentException("This application has already registered handler: " + ProcessedUriInfo);
                    }
                }
                appNames_.Add(StarcounterEnvironment.AppName);
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

            dontMerge_ = ho.DontMerge;

            if (param_message_type != null)
                uri_info_.param_message_create_ = Expression.Lambda<Func<object>>(Expression.New(param_message_type)).Compile();

            Debug.Assert(userDelegates_ == null);

            userDelegates_ = new List<Func<Request,IntPtr,IntPtr,Response>>();
            userDelegates_.Add(user_delegate);

            appNames_ = new List<String>();

            appNames_.Add(StarcounterEnvironment.AppName);

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

        public static Func<Request, List<Response>, Response> ResponsesMergerRoutine_;

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
                        allUriHandlers_[i].AddDelegateToList(wrappedDelegate, ho);
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

                        String appNames = allUriHandlers_[handlerId].AppNames;
                        if (String.IsNullOrEmpty(appNames)) {
                            appNames = MixedCodeConstants.EmptyAppName;
                        }

                        UriInjectMethods.RegisterUriHandlerNative_(
                            port,
                            appNames,
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