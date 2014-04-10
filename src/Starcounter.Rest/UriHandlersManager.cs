using Starcounter;
using Starcounter.Internal;
using Starcounter.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Starcounter.Rest
{
    /// <summary>
    /// Handlers API.
    /// </summary>
    public class Handlers {
        internal static void AddExtraHandlerLevel() {
            HandlerOptions.NumHandlerLevels++;
            UriHandlersManager.AddExtraHandlerLevel(false);
        }
    }

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
        public Byte[] native_param_types_ = null;
        public Byte num_params_ = 0;
        public UInt16 handler_id_ = UInt16.MaxValue;
        public UInt64 handler_info_ = UInt64.MaxValue;
        public UInt16 port_ = 0;
        public MixedCodeConstants.NetworkProtocolType proto_type_ = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1;
        public HTTP_METHODS http_method_ = HTTP_METHODS.GET;

        public void Destroy()
        {
            original_uri_info_ = null;
            processed_uri_info_ = null;
            handler_id_ = UInt16.MaxValue;

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

        public void InitUriPointers()
        {
            unsafe
            {
                original_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(original_uri_info_.Length + 1);
                Byte[] temp = Encoding.ASCII.GetBytes(original_uri_info_);
                fixed (Byte* t = temp)
                {
                    BitsAndBytes.MemCpy((Byte*)original_uri_info_ascii_bytes_, t, (uint)original_uri_info_.Length);
                }

                ((Byte*)original_uri_info_ascii_bytes_)[original_uri_info_.Length] = 0;

                processed_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(processed_uri_info_.Length + 1);
                temp = Encoding.ASCII.GetBytes(processed_uri_info_);
                fixed (Byte* t = temp)
                {
                    BitsAndBytes.MemCpy((Byte*)processed_uri_info_ascii_bytes_, t, (uint)processed_uri_info_.Length);
                }

                ((Byte*)processed_uri_info_ascii_bytes_)[processed_uri_info_.Length] = 0;
            }
        }

        public unsafe MixedCodeConstants.RegisteredUriManaged GetRegisteredUriManaged()
        {
            MixedCodeConstants.RegisteredUriManaged r = new MixedCodeConstants.RegisteredUriManaged();

            r.original_uri_info_string = original_uri_info_ascii_bytes_;
            r.processed_uri_info_string = processed_uri_info_ascii_bytes_;

            r.num_params = num_params_;

            // TODO: Resolve this hack with only positive handler ids in generated code.
            r.handler_id = handler_id_ + 1;

            for (Int32 i = 0; i < native_param_types_.Length; i++)
                r.param_types[i] = native_param_types_[i];

            return r;
        }
    }

    /// <summary>
    /// User handler information.
    /// </summary>
    internal class UserHandlerInfo
    {
        /// <summary>
        /// List of user delegates.
        /// </summary>
        List<Func<Request, IntPtr, IntPtr, Response>> userDelegates_ = null;

        /// <summary>
        /// List of application names.
        /// </summary>
        List<String> appNames_ = null;

        /// <summary>
        /// Mapper handler index.
        /// </summary>
        Int32 mapperHandlerIndex_ = -1;

        /// <summary>
        /// Checks if response is in local cache.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        Response TryGetResponseFromCache(Request req) {
            // Checking if we are in session already.
            if (!req.IsInternal) {

                // Setting the original request.
                Session.InitialRequest = req;

                // Obtaining session.
                Session s = (Session)req.GetAppsSessionInterface();

                // Checking if correct session was obtained.
                if ((req.CameWithCorrectSession) && (null != s)) {

                    // Starting session.
                    Session.Start(s);

                    // Checking if we can reuse the cache.
                    Response resp;
                    if ((req.IsInternal) && (X.CheckLocalCache(req.Uri, null, null, out resp))) {

                        // Setting the session again.
                        resp.AppsSession = Session.Current.InternalSession;

                        return resp;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checking if response should be added to cache.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="resp"></param>
        void TryAddResponseToCache(Request req, Response resp) {
            // In case of returned JSON object within current session we need to save it
            // for later reuse.

            Json rootJsonObj = null;
            if (null != Session.Current)
                rootJsonObj = Session.Current.Data;

            Json curJsonObj = null;

            // Checking if response is processed later.
            if (resp.HandlingStatus == HandlerStatusInternal.Handled)
                return;

            // Setting session on result only if its original request.
            if ((null != Session.Current) && (!req.IsInternal) && (!req.CameWithCorrectSession))
                resp.AppsSession = Session.Current.InternalSession;

            // Converting response to JSON.
            curJsonObj = resp;

            if ((null != curJsonObj) &&
                (null != rootJsonObj) &&
                (req.IsCachable()) &&
                (curJsonObj.HasThisRoot(rootJsonObj))) {
                Session.Current.AddJsonNodeToCache(req.Uri, curJsonObj);
            }
        }
        
        /// <summary>
        /// Runs all user delegates.
        /// </summary>
        /// <param name="req">Original request.</param>
        /// <param name="methodAndUri">Method and URI pointer.</param>
        /// <param name="rawParamsInfo">Raw parameters info.</param>
        /// <returns>Response or merged response.</returns>
        public Response RunUserDelegates(Request req, IntPtr methodAndUri, IntPtr rawParamsInfo, UriHandlersManager uhm) {

            Debug.Assert(userDelegates_ != null);

            // Checking if there is only one delegate or merge function is not defined.
            if ((uhm.ResponsesMergerRoutine_ == null) ||
                (userDelegates_.Count == 1)) {

                // Setting current application name.
                StarcounterEnvironment.AppName = appNames_[0];

                // Checking local cache.
                Response resp = TryGetResponseFromCache(req);

                if (null == resp) {
                    // Calling intermediate user delegate.
                    resp = userDelegates_[0](req, methodAndUri, rawParamsInfo);

                    // Check if response should be added to cache.
                    TryAddResponseToCache(req, resp);
                }

                return resp;
            }
            
            List<Response> responses = new List<Response>();

            // Checking if we have an external call and mapper handler defined.
            if ((mapperHandlerIndex_ >= 0) && (!Handle.CallOnlyNonMapperHandlers)) {
                
                StarcounterEnvironment.AppName = appNames_[mapperHandlerIndex_];
                StarcounterEnvironment.OrigMapperCallerAppName = appNames_[mapperHandlerIndex_];
                Response resp = userDelegates_[mapperHandlerIndex_](req, methodAndUri, rawParamsInfo);

                return resp;
            }

            // Running every delegate from the list.
            for (int i = 0; i < userDelegates_.Count; i++) {
                var func = userDelegates_[i];

                // If handler is a mapper and we in non-mapper mode - just skip this handler.
                if (Handle.CallOnlyNonMapperHandlers && (mapperHandlerIndex_ == i))
                    continue;

                // Setting application name.
                StarcounterEnvironment.AppName = appNames_[i];

                // Checking local cache.
                Response resp = TryGetResponseFromCache(req);

                if (null == resp) {
                    // Calling intermediate user delegate.
                    resp = func(req, methodAndUri, rawParamsInfo);

                    // Check if response should be added to cache.
                    TryAddResponseToCache(req, resp);
                }

                // Setting to which application the response belongs.
                resp.AppName = appNames_[i];

                // The first response is the one we should merge on.
                if (appNames_[i] == StarcounterEnvironment.OrigMapperCallerAppName)
                    responses.Insert(0, resp);
                else
                    responses.Add(resp);
            }

            Handle.CallOnlyNonMapperHandlers = false;

            // Checking if we have a response merging function defined.
            if (responses.Count > 1) {
                Debug.Assert(uhm.ResponsesMergerRoutine_ != null);

                // Creating merged response.
                return uhm.ResponsesMergerRoutine_(req, responses);

            } else {

                return responses[0];
            }
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

        public void AddDelegateToList(Func<Request, IntPtr, IntPtr, Response> user_delegate)
        {
            userDelegates_.Add(user_delegate);
            appNames_.Add(StarcounterEnvironment.AppName);

            if (Handle.IsMapperHandler) {
                mapperHandlerIndex_ = appNames_.Count - 1;
                Handle.IsMapperHandler = false;
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
            MixedCodeConstants.NetworkProtocolType protoType)
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

            Debug.Assert(userDelegates_ == null);

            userDelegates_ = new List<Func<Request,IntPtr,IntPtr,Response>>();
            userDelegates_.Add(user_delegate);

            appNames_ = new List<String>();
            appNames_.Add(StarcounterEnvironment.AppName);

            if (Handle.IsMapperHandler)
            {
                mapperHandlerIndex_ = appNames_.Count - 1;
                Handle.IsMapperHandler = false;
            }

            uri_info_.InitUriPointers();
        }
    }

    public class UriInjectMethods {

        public static Func<Request, Int32, Response> HandleInternalRequest_;
        public static Func<Request, Boolean> OnHttpMessageRoot_;
        public static Action<string, ushort> OnHandlerRegistered_;
        public static bmx.BMX_HANDLER_CALLBACK HttpOuterHandler_;

        public static void SetHandlerRegisteredCallback(Action<string, ushort> callback) {
            OnHandlerRegistered_ = callback;
        }

        // Checking if this Node supports local resting.
        internal static Boolean IsSupportingLocalNodeResting() {
            return HandleInternalRequest_ != null;
        }

        public static void SetRegisterUriHandlerNew(
            bmx.BMX_HANDLER_CALLBACK httpOuterHandler,
            Func<Request, Boolean> onHttpMessageRoot,
            Func<Request, Int32, Response> handleInternalRequest) {

            HttpOuterHandler_ = httpOuterHandler;
            OnHttpMessageRoot_ = onHttpMessageRoot;
            HandleInternalRequest_ = handleInternalRequest;
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

        Int32 maxNumHandlersEntries_ = 0;

        Boolean registerWithGateway_ = false;

        /// <summary>
        /// Indicates if this Uri handlers manager should register with gateway.
        /// </summary>
        public Boolean RegisterWithGateway {
            set { registerWithGateway_ = value; }
        }

        internal Func<Request, List<Response>, Response> ResponsesMergerRoutine_;

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
            lock (UriInjectMethods.HandleInternalRequest_)
            {
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

        static List<UriHandlersManager> uriHandlersManagers_ = new List<UriHandlersManager>();

        public static UriHandlersManager GetUriHandlersManager(Int32 handlersIndex) {
            return uriHandlersManagers_[handlersIndex];
        }

        internal static void AddExtraHandlerLevel(Boolean registerWithGateway) {
            uriHandlersManagers_.Add(new UriHandlersManager(registerWithGateway));
        }

        internal static void ResetUriHandlersManagers() {
            foreach (UriHandlersManager uhm in uriHandlersManagers_)
                uhm.Reset();

            uriHandlersManagers_ = new List<UriHandlersManager>();

            AddExtraHandlerLevel(false);
        }

        public UserHandlerInfo[] AllUserHandlerInfos
        {
            get { return allUriHandlers_; }
        }

        public Int32 NumRegisteredHandlers
        {
            get { return maxNumHandlersEntries_; }
        }

        public const Int32 MAX_URI_HANDLERS = 1024;

        internal void Reset()
        {
            maxNumHandlersEntries_ = 0;

            portUris_ = new List<PortUris>();

            allUriHandlers_ = new UserHandlerInfo[MAX_URI_HANDLERS];
            for (Int32 i = 0; i < MAX_URI_HANDLERS; i++)
                allUriHandlers_[i] = new UserHandlerInfo();
        }

        public UriHandlersManager(Boolean registerWithGateway)
        {
            // Initializing port uris.
            PortUris.GlobalInit();

            allUriHandlers_ = new UserHandlerInfo[MAX_URI_HANDLERS];
            for (Int32 i = 0; i < MAX_URI_HANDLERS; i++)
                allUriHandlers_[i] = new UserHandlerInfo();

            registerWithGateway_ = registerWithGateway;
        }

        public void RunDelegate(Request r, out Response resp)
        {
            unsafe
            {
                UserHandlerInfo uhi = allUriHandlers_[r.ManagedHandlerId];

                // Checking if we had custom type user Message argument.
                if (uhi.ArgMessageType != null)
                    r.ArgMessageObjectType = uhi.ArgMessageType;

                // Setting some request parameters.
                r.PortNumber = uhi.UriInfo.port_;
                r.MethodEnum = uhi.UriInfo.http_method_;

                // Saving original application name.
                String origAppName = StarcounterEnvironment.AppName;

                // Calling user delegate.
                resp = uhi.RunUserDelegates(
                    r,
                    r.GetRawMethodAndUri(),
                    r.GetRawParametersInfo(),
                    this);

                // Setting back the application name.
                StarcounterEnvironment.AppName = origAppName;
            }
        }

        void UnregisterUriHandler(UInt16 port, String originalUriInfo)
        {
            // Ensuring correct multi-threading handlers creation.
            lock (allUriHandlers_)
            {
                UInt32 errorCode = bmx.sc_bmx_unregister_uri(port, originalUriInfo);
                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "URI string: " + originalUriInfo);
            }
        }

        void RegisterUriHandlerNative(
            UInt16 port,
            String appName,
            String originalUriInfo,
            String processedUriInfo,
            Byte[] paramTypes,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
        {
            Byte numParams = 0;
            if (null != paramTypes)
                numParams = (Byte)paramTypes.Length;

            unsafe
            {
                fixed (Byte* pp = paramTypes)
                {
                    UInt32 errorCode = bmx.sc_bmx_register_uri_handler(
                        port,
                        appName,
                        originalUriInfo,
                        processedUriInfo,
                        pp,
                        numParams,
                        UriInjectMethods.HttpOuterHandler_,
                        managedHandlerIndex,
                        out handlerInfo);

                    if (errorCode != 0)
                        throw ErrorCode.ToException(errorCode, "URI string: " + originalUriInfo);
                }
            }
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
            MixedCodeConstants.NetworkProtocolType protoType)
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
                        allUriHandlers_[i].AddDelegateToList(wrappedDelegate);
                        return;
                    }
                }

                // TODO: Search for unoccupied slot.
                handlerId = (UInt16)maxNumHandlersEntries_;
                maxNumHandlersEntries_++;

                UInt64 handlerInfo = UInt64.MaxValue;

                if (handlerId >= MAX_URI_HANDLERS)
                    throw new ArgumentOutOfRangeException("Too many user handlers registered!");

                allUriHandlers_[handlerId].Init(
                    port,
                    originalUriInfo,
                    processedUriInfo,
                    wrappedDelegate,
                    nativeParamTypes,
                    messageType,
                    handlerId,
                    handlerInfo,
                    protoType);

                // Registering the outer native handler (if any).
                if (UriInjectMethods.HttpOuterHandler_ != null) {
                    if (registerWithGateway_) {
                        RegisterUriHandlerNative(
                            port,
                            allUriHandlers_[handlerId].AppNames,
                            originalUriInfo,
                            processedUriInfo,
                            nativeParamTypes,
                            handlerId,
                            out handlerInfo);
                    }
                }

                // Updating handler info from received value.
                allUriHandlers_[handlerId].UriInfo.handler_info_ = handlerInfo;

                if (UriInjectMethods.OnHandlerRegistered_ != null)
                    UriInjectMethods.OnHandlerRegistered_(originalUriInfo, port);

                // Resetting port URIs matcher.
                PortUris portUris = SearchPort(port);
                if (portUris != null)
                    portUris.MatchUriAndGetHandlerId = null;
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
                for (Int32 i = 0; i < MAX_URI_HANDLERS; i++)
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