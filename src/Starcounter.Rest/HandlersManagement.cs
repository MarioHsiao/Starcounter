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
                original_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(original_uri_info_.Length);
                Byte[] temp = Encoding.ASCII.GetBytes(original_uri_info_);
                fixed (Byte* t = temp)
                {
                    BitsAndBytes.MemCpy((Byte*)original_uri_info_ascii_bytes_, t, (uint)original_uri_info_.Length);
                }

                processed_uri_info_ascii_bytes_ = BitsAndBytes.Alloc(processed_uri_info_.Length);
                temp = Encoding.ASCII.GetBytes(processed_uri_info_);
                fixed (Byte* t = temp)
                {
                    BitsAndBytes.MemCpy((Byte*)processed_uri_info_ascii_bytes_, t, (uint)processed_uri_info_.Length);
                }
            }
        }

        public unsafe MixedCodeConstants.RegisteredUriManaged GetRegisteredUriManaged()
        {
            MixedCodeConstants.RegisteredUriManaged r = new MixedCodeConstants.RegisteredUriManaged();

            r.original_uri_info_len_chars = (UInt32)original_uri_info_.Length;
            r.original_uri_info_string = original_uri_info_ascii_bytes_;

            r.processed_uri_info_len_chars = (UInt32)processed_uri_info_.Length;
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
        List<Func<Request, IntPtr, IntPtr, Response>> user_delegates_ = null;

        /// <summary>
        /// List of application names.
        /// </summary>
        List<String> app_names_ = null;

        /// <summary>
        /// Runs all user delegates.
        /// </summary>
        /// <param name="req">Original request.</param>
        /// <param name="methodAndUri">Method and URI pointer.</param>
        /// <param name="rawParamsInfo">Raw parameters info.</param>
        /// <returns>Response or merged response.</returns>
        public Response RunUserDelegates(Request req, IntPtr methodAndUri, IntPtr rawParamsInfo) {

            Debug.Assert(user_delegates_ != null);

            // Checking if there is only one delegate.
            if (user_delegates_.Count == 1)
                return user_delegates_[0](req, methodAndUri, rawParamsInfo);
            
            List<Response> responses = new List<Response>();

            // Running every delegate from the list.
            foreach(Func<Request, IntPtr, IntPtr, Response> func in user_delegates_) {
                responses.Add(func(req, methodAndUri, rawParamsInfo));
            }

            Debug.Assert(UserHandlerCodegen.HandlersManager.ResponsesMergerRoutine_ != null);

            // Creating merged response.
            return UserHandlerCodegen.HandlersManager.ResponsesMergerRoutine_(req, responses, app_names_);
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
            user_delegates_ = null;
        }

        public void AddDelegateToList(Func<Request, IntPtr, IntPtr, Response> user_delegate)
        {
            user_delegates_.Add(user_delegate);
            app_names_.Add(StarcounterEnvironment.AppName);
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

            Debug.Assert(user_delegates_ == null);

            user_delegates_ = new List<Func<Request,IntPtr,IntPtr,Response>>();
            user_delegates_.Add(user_delegate);

            app_names_ = new List<String>();
            app_names_.Add(StarcounterEnvironment.AppName);

            uri_info_.InitUriPointers();
        }
    }

    /// <summary>
    /// All registered handlers manager.
    /// </summary>
    internal class HandlersManagement
    {
        UserHandlerInfo[] allUriHandlers_ = null;

        List<PortUris> portUris_ = new List<PortUris>();

        Int32 maxNumHandlersEntries_ = 0;

        internal Func<Request, List<Response>, List<String>, Response> ResponsesMergerRoutine_;

        public HandleInternalRequestDelegate HandleInternalRequest_;

        public delegate Boolean UriCallbackDelegate(Request req);

        public delegate Response HandleInternalRequestDelegate(Request request);

        // Checking if this Node supports local resting.
        internal Boolean IsSupportingLocalNodeResting()
        {
            return HandleInternalRequest_ != null;
        }

        public static UriCallbackDelegate OnHttpMessageRoot_;
        static Action<string, ushort> OnHandlerRegistered_;
        bmx.BMX_HANDLER_CALLBACK HttpOuterHandler_;

        public static void SetHandlerRegisteredCallback(Action<string, ushort> callback)
        {
            OnHandlerRegistered_ = callback;
        }

        public void SetRegisterUriHandlerNew(
            bmx.BMX_HANDLER_CALLBACK httpOuterHandler,
            UriCallbackDelegate onHttpMessageRoot,
            HandleInternalRequestDelegate handleInternalRequest)
        {
            HttpOuterHandler_ = httpOuterHandler;
            OnHttpMessageRoot_ = onHttpMessageRoot;
            HandleInternalRequest_ = handleInternalRequest;
        }

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
            lock (HandleInternalRequest_)
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

        public HandlersManagement()
        {
            // Initializing port uris.
            PortUris.GlobalInit();

            allUriHandlers_ = new UserHandlerInfo[MAX_URI_HANDLERS];
            for (Int32 i = 0; i < MAX_URI_HANDLERS; i++)
                allUriHandlers_[i] = new UserHandlerInfo();
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

                // Calling user delegate.
                resp = uhi.RunUserDelegates(
                    r,
                    r.GetRawMethodAndUri(),
                    r.GetRawParametersInfo());
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
                        originalUriInfo,
                        processedUriInfo,
                        pp,
                        numParams,
                        HttpOuterHandler_,
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
                        if (ResponsesMergerRoutine_ == null)
                        {
                            throw ErrorCode.ToException(Error.SCERRHANDLERALREADYREGISTERED, "Processed URI: " + processedUriInfo);
                        }
                        else
                        {
                            allUriHandlers_[i].AddDelegateToList(wrappedDelegate);
                            return;
                        }
                    }
                }

                // TODO: Search for unoccupied slot.
                handlerId = (UInt16)maxNumHandlersEntries_;
                maxNumHandlersEntries_++;

                UInt64 handlerInfo = UInt64.MaxValue;

                // Registering the outer native handler (if any).
                if (HttpOuterHandler_ != null)
                {
                    RegisterUriHandlerNative(
                        port,
                        originalUriInfo,
                        processedUriInfo,
                        nativeParamTypes,
                        handlerId,
                        out handlerInfo);
                }

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

                if (OnHandlerRegistered_ != null)
                    OnHandlerRegistered_(originalUriInfo, port);

                // Resetting port URIs matcher.
                PortUris portUris = UserHandlerCodegen.HandlersManager.SearchPort(port);
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

        /// <summary>
        /// Incorrect session exception.
        /// </summary>
        public class IncorrectSessionException : Exception { }
    }
}