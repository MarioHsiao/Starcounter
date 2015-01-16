#define CASE_INSENSITIVE_URI_MATCHER

using Starcounter.Advanced;
using Starcounter.Internal.Uri;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Codeplex.Data;
using System.IO;
using System.Globalization;
using Starcounter.Logging;
using System.Net;

namespace Starcounter.Rest
{
    internal class UriManagedHandlersCodegen
    {
        unsafe static Int32 USER_PARAM_INFO_SIZE = sizeof(MixedCodeConstants.UserDelegateParamInfo);

        public UriManagedHandlersCodegen()
        {
            if (USER_PARAM_INFO_SIZE != 4)
                throw new Exception("User param info size != 4");
        }

        public static unsafe Int64 FastParseInt(IntPtr ptr, int numChars)
        {
            Int64 mult = 1, result = 0;
            Boolean neg = false;

            unsafe
            {
                Byte* start = (Byte*)ptr;
                if (*start == (Byte)'-')
                    neg = true;
                else
                    start--;

                Byte* cur = (Byte*)ptr + numChars - 1;
                do
                {
                    result += mult * (*cur - (Byte)'0');
                    mult *= 10;
                    cur--;
                }
                while (cur > start);

                if (neg)
                    result = -result;

                return result;
            }
        }

        public static Int32 ReadInt32Param(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 16))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return (Int32)FastParseInt(dataBegin + p.offset_, p.len_);
            }
        }

        public static Int64 ReadInt64Param(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 16))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return FastParseInt(dataBegin + p.offset_, p.len_);
            }
        }

        public static String ReadStringParam(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > req.GetRequestLength()))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return Marshal.PtrToStringAnsi(dataBegin + p.offset_, p.len_);
            }
        }

        public static Boolean ReadBooleanParam(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 5))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                Byte b = *(((Byte*)dataBegin) + p.offset_);
                if (b == 't' || b == 'T')
                    return true;

                return false;
            }
        }

        public static Decimal ReadDecimalParam(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 32))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return Decimal.Parse(Marshal.PtrToStringAnsi(dataBegin + p.offset_, p.len_), CultureInfo.InvariantCulture);
            }
        }

        public static Double ReadDoubleParam(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 32))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return Double.Parse(Marshal.PtrToStringAnsi(dataBegin + p.offset_, p.len_), CultureInfo.InvariantCulture);
            }
        }

        public static DateTime ReadDateTimeParam(Request req, IntPtr dataBegin, IntPtr paramsInfo)
        {
            unsafe
            {
                MixedCodeConstants.UserDelegateParamInfo p = *(MixedCodeConstants.UserDelegateParamInfo*)paramsInfo;

                // Checking for correct handler info.
                if ((p.offset_ >= req.GetRequestLength()) || (p.len_ > 32))
                    throw new ArgumentOutOfRangeException("Wrong handler called for request: " + req.Uri);

                return DateTime.Parse(Marshal.PtrToStringAnsi(dataBegin + p.offset_, p.len_), CultureInfo.InvariantCulture);
            }
        }

        public static Json ReadMessageParam(Request req)
        {
            unsafe
            {
                IntPtr bodyPtr;
                uint bodySize;
                Json m = (Json)req.ArgMessageObjectCreate();

                req.GetBodyRaw(out bodyPtr, out bodySize);
                m.PopulateFromJson(bodyPtr, (int)bodySize);

                return m;
            }
        }

        public static DynamicJson ReadDynamicJsonParam(Request req)
        {
            unsafe
            {
                return DynamicJson.Parse(req.Body);
            }
        }

        public static IAppsSession ReadSessionParam(Request req)
        {
            unsafe
            {
                IAppsSession session = req.GetAppsSessionInterface();

                // Checking if session is active.
                if (null == session)
                    throw new UriInjectMethods.IncorrectSessionException();

                return session;
            }
        }

        /// <summary>
        /// Generates main LINQ parameters parsing code.
        /// </summary>
        public void GenerateMainParamsParsingCode(
            List<RestDelegateArgumentTypes> delegateArgTypes,
            ParameterExpression req,
            ParameterExpression dataBeginPtr,
            ParameterExpression paramsInfoPtr,
            Type argMessageType,
            Type argSessionType,
            ref List<Expression> bodyExpressions,
            ref List<ParameterExpression> parsedParams)
        {
            foreach (RestDelegateArgumentTypes argType in delegateArgTypes)
            {
                switch (argType)
                {
                    case RestDelegateArgumentTypes.REST_ARG_STRING:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadStringParam"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(String));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_INT32:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadInt32Param"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Int32));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_INT64:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadInt64Param"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Int64));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_DECIMAL:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadDecimalParam"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Decimal));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_DOUBLE:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadDoubleParam"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Double));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_BOOLEAN:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadBooleanParam"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Boolean));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_DATETIME:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadDateTimeParam"),
                            req,
                            dataBeginPtr,
                            paramsInfoPtr);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(DateTime));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_HTTPREQUEST:
                    {
                        parsedParams.Add(req);
                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_MESSAGE:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadMessageParam"),
                            req);

                        ParameterExpression parsedVar = Expression.Parameter(argMessageType);
                        bodyExpressions.Add(Expression.Assign(parsedVar, Expression.Convert(methodCall, argMessageType)));

                        parsedParams.Add(parsedVar);

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_DYNAMIC_JSON:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadDynamicJsonParam"),
                            req);

                        ParameterExpression parsedVar = Expression.Parameter(typeof(Object));
                        bodyExpressions.Add(Expression.Assign(parsedVar, methodCall));

                        parsedParams.Add(parsedVar);

                        break;
                    }

                    case RestDelegateArgumentTypes.REST_ARG_SESSION:
                    {
                        MethodCallExpression methodCall = Expression.Call(
                            typeof(UriManagedHandlersCodegen).GetMethod("ReadSessionParam"),
                            req);

                        ParameterExpression parsedVar = Expression.Parameter(argSessionType);
                        bodyExpressions.Add(Expression.Assign(parsedVar, Expression.Convert(methodCall, argSessionType)));

                        parsedParams.Add(parsedVar);

                        // Advancing the pointer to parameter data.
                        bodyExpressions.Add(Expression.AddAssign(paramsInfoPtr, Expression.Constant(USER_PARAM_INFO_SIZE)));

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Tries to find handler by processed URI string.
        /// </summary>
        public static UserHandlerInfo FindHandlerByProcessedUri(String processedUriInfo, HandlerOptions ho = null) {

            // Checking if handler options is defined.
            if (ho == null) {
                ho = HandlerOptions.DefaultHandlerOptions;
            }

            return UriHandlersManager.GetUriHandlersManager(ho.HandlerLevel).FindHandlerByProcessedUri(processedUriInfo);
        }

        /// <summary>
        /// Does internal registration of delegate involving notification of gateway.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> RegisterDelegate(
            UInt16 port,
            String methodAndUriInfo,
            MethodInfo userDelegateInfo,
            Expression delegExpr,
            MixedCodeConstants.NetworkProtocolType protoType,
            HandlerOptions ho)
        {
            String[] s = methodAndUriInfo.Split(null);
            String originalUriInfo = null;
            String polyjuiceMsg = "Error registering handler: " + methodAndUriInfo + ". Polyjuice applications can only register handlers starting with application name prefix, for example, /myapp/foo";

            // Checking if consists of method and URI.
            if (s.Length > 1) {

                // Checking that HTTP method is upper case.
                if ((s[0] != s[0].ToUpperInvariant())) {
                    throw new ArgumentOutOfRangeException("Handler HTTP method should be upper-case (HTTP 1.1 RFC).");
                }

#if CASE_INSENSITIVE_URI_MATCHER
                s[1] = s[1].ToLowerInvariant();
#endif
                // Checking if its a Polyjuice application.
                if (StarcounterEnvironment.PolyjuiceAppsFlag) {

                    // Checking that its a Polyjuice handler.
                    if ((ho == null) || (false == ho.AllowNonPolyjuiceHandler)) {

                        // Handler name should start with application name or launcher name.
                        if (!s[1].StartsWith("/" + StarcounterEnvironment.AppName, StringComparison.InvariantCultureIgnoreCase) &&
                            !s[1].StartsWith("/" + StarcounterConstants.LauncherAppName, StringComparison.InvariantCultureIgnoreCase) &&
                            !s[1].StartsWith("/" + StarcounterConstants.SocietyObjectsPrefix, StringComparison.InvariantCultureIgnoreCase) &&
                            !s[1].StartsWith("/__db/", StringComparison.InvariantCultureIgnoreCase)) {
                            throw new ArgumentOutOfRangeException(polyjuiceMsg);
                        }
                    }
                }

                // Constructing original URI info.
                originalUriInfo = s[0] + " " + s[1];

            } else {

                // Checking if its a Polyjuice application.
                if (StarcounterEnvironment.PolyjuiceAppsFlag) {

                    // Checking that its a Polyjuice handler.
                    if ((ho == null) || (false == ho.AllowNonPolyjuiceHandler)) {

                        throw new ArgumentOutOfRangeException(polyjuiceMsg);
                    }
                }

#if CASE_INSENSITIVE_URI_MATCHER
                s[0] = s[0].ToLowerInvariant();
#endif

                originalUriInfo = s[0];
            }

            // Checking if handler options is defined.
            if (ho == null) {
                ho = HandlerOptions.DefaultHandlerOptions;
            }

            // Mutually excluding handler registrations.
            Byte[] nativeParamTypes;
            String processedUriInfo;
            Type argMessageType;
            Type argSessionType;

            // Generating callback.
            Func<Request, IntPtr, IntPtr, Response> wrappedDelegate = GenerateParsingDelegateAndGetParameters(
                originalUriInfo,
                userDelegateInfo,
                delegExpr,
                out nativeParamTypes,
                out processedUriInfo,
                out argMessageType,
                out argSessionType);

            // Registering handler with gateway and getting the id.
            UriHandlersManager.GetUriHandlersManager(ho.HandlerLevel).RegisterUriHandler(
                port,
                originalUriInfo,
                processedUriInfo,
                nativeParamTypes,
                argMessageType,
                wrappedDelegate,
                protoType,
                ho);

            return wrappedDelegate;
        }

        /// <summary>
        /// Generates code using LINQ expressions for calling user delegate.
        /// </summary>
        Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegateAndGetParameters(
            String originalUriInfo,
            MethodInfo userDelegateInfo,
            Expression delegExpr,
            out Byte[] nativeParamTypes,
            out String processedUriInfo,
            out Type argMessageType,
            out Type argSessionType)
        {
            // Checking that URI is correctly formed.
            Int32 spaceAfterMethodIndex = originalUriInfo.IndexOf(' ');
            String relativeUri = originalUriInfo.Substring(spaceAfterMethodIndex + 1);
            if (!System.Uri.IsWellFormedUriString(relativeUri.Replace(Handle.UriParameterIndicator, "XXX"), UriKind.Relative))
                throw new ArgumentException("Handler relative URI: \"" + relativeUri + "\" is ill formed. Please consult RFC 3986 for more information.");

            List<RestDelegateArgumentTypes> delegateArgTypes = new List<RestDelegateArgumentTypes>(),
                userParameterTypes = new List<RestDelegateArgumentTypes>();

            ParameterInfo[] allParams = userDelegateInfo.GetParameters();

            List<Byte> nativeParamsTypesList = new List<Byte>();
            nativeParamTypes = new Byte[allParams.Length];

            RequestProcessorMetaData rp = new RequestProcessorMetaData();

            argMessageType = null;
            argSessionType = null;

            Int32 numPureNativeParams = originalUriInfo.Split(new String[] { Handle.UriParameterIndicator }, StringSplitOptions.None).Length - 1;
            Int32 numPureManagedParams = 0;
            Boolean hasSessionParam = false;
            foreach (ParameterInfo p in allParams)
            {
                // Calculating purely managed parameters.
                if ((p.ParameterType == typeof(Request)) ||
                    (p.ParameterType.IsSubclassOf(typeof(Json))) ||
                    (p.ParameterType == typeof(Object)))
                {
                    numPureManagedParams++;
                }
                else if (p.ParameterType == typeof(Session))
                {
                    hasSessionParam = true;
                }
            }

            // Checking that URI is constructed correctly.
            if (numPureNativeParams != allParams.Length - numPureManagedParams)
            {
                Int32 numExtraParams = allParams.Length - numPureManagedParams - numPureNativeParams;
                if (numExtraParams > 0)
                {
                    if ((numExtraParams != 1) || (!hasSessionParam))
                        throw new ArgumentException("Handler URI: \"" + relativeUri + "\" and handler parameters do not correspond to each other. Please check that your URI parameters {?} correspond to your delegate arguments.");
                }
            }

            // Checking other conditions.
            if ((allParams.Length - numPureNativeParams - numPureManagedParams) < 0)
                throw new ArgumentException("Handler URI: \"" + relativeUri + "\" parameters like Request and Json should not have corresponding URI parameters {?}.");

            // Determining if session is pure managed code thing.
            Boolean isSessionPureManaged = false;
            if ((allParams.Length - numPureNativeParams) > numPureManagedParams)
                isSessionPureManaged = true;

            // Checking parameter types.
            foreach (ParameterInfo p in allParams)
            {
                if (p.ParameterType == typeof(Int32))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_INT32);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_INT32);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_INT32);
                }
                else if (p.ParameterType == typeof(String))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_STRING);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_STRING);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_STRING);
                }
                else if (p.ParameterType == typeof(Decimal))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_DECIMAL);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_DECIMAL);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_DECIMAL);
                }
                else if (p.ParameterType == typeof(Int64))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_INT64);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_INT64);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_INT64);
                }
                else if (p.ParameterType == typeof(Double))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_DOUBLE);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_DOUBLE);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_DOUBLE);
                }
                else if (p.ParameterType == typeof(Boolean))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_BOOLEAN);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_BOOLEAN);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_BOOLEAN);
                }
                else if (p.ParameterType == typeof(DateTime))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_DATETIME);
                    nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_DATETIME);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_DATETIME);
                }
                else if (typeof(IAppsSession).IsAssignableFrom(p.ParameterType))
                {
                    // Getting concrete type of a message.
                    argSessionType = p.ParameterType;

                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_SESSION);
                    userParameterTypes.Add(RestDelegateArgumentTypes.REST_ARG_SESSION);

                    // Checking if session is purely managed.
                    if (!isSessionPureManaged)
                        nativeParamsTypesList.Add((Byte)RestDelegateArgumentTypes.REST_ARG_SESSION);
                }
                else if (p.ParameterType == typeof(Request))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_HTTPREQUEST);

                    // Not adding Request as parameter here since its a pure managed code thing.
                    continue;
                }
                else if (p.ParameterType.IsSubclassOf(typeof(Json)))
                {
                    // Getting concrete type of a message.
                    argMessageType = p.ParameterType;

                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_MESSAGE);

                    // Not adding Json as parameter here since its a pure managed code thing.
                    continue;
                }
                else if (p.ParameterType == typeof(Object))
                {
                    delegateArgTypes.Add(RestDelegateArgumentTypes.REST_ARG_DYNAMIC_JSON);

                    // Not adding Object as parameter here since its a pure managed code thing.
                    continue;
                }
            }

            nativeParamTypes = nativeParamsTypesList.ToArray();

            processedUriInfo = UriTemplatePreprocessor.GetProcessedUriInfoFromOriginal(originalUriInfo, userParameterTypes);

            ParameterExpression httpRequest = Expression.Parameter(typeof(Request));
            ParameterExpression paramsDataPtr = Expression.Parameter(typeof(IntPtr));
            ParameterExpression paramsInfoPtr = Expression.Parameter(typeof(IntPtr));

            List<Expression> bodyExpressions = new List<Expression>();
            List<ParameterExpression> parsedParams = new List<ParameterExpression>();

            GenerateMainParamsParsingCode(
                delegateArgTypes,
                httpRequest,
                paramsDataPtr,
                paramsInfoPtr,
                argMessageType,
                argSessionType,
                ref bodyExpressions,
                ref parsedParams);

            Expression callUserDelegate = null;
            if (userDelegateInfo.IsStatic)
                callUserDelegate = Expression.Call(userDelegateInfo, parsedParams);
            else
                callUserDelegate = Expression.Invoke(delegExpr, parsedParams);

            LabelTarget returnTarget = Expression.Label(typeof(Response));
            bodyExpressions.Add(Expression.Return(returnTarget, callUserDelegate));

            bodyExpressions.Add(Expression.Label(returnTarget, Expression.Constant(null, typeof(Response))));

            // Removing the Request from local variables since its passed as a parameter.
            parsedParams.Remove(httpRequest);

            // Creating block with function parameters.
            BlockExpression wholeFunction = Expression.Block(parsedParams, bodyExpressions);

            LambdaExpression lambdaExpr = Expression.Lambda<Func<Request, IntPtr, IntPtr, Response>>(wholeFunction, httpRequest, paramsDataPtr, paramsInfoPtr);

            return (Func<Request, IntPtr, IntPtr, Response>)lambdaExpr.Compile();
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate(
            UInt16 port,
            String methodAndUriInfo,
            Func<Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<Response>> delegExpr = () => userDelegate();
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, Response>> delegExpr = (p1) => userDelegate(p1);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, Response>> delegExpr = (p1, p2) => userDelegate(p1, p2);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, Response>> delegExpr = (p1, p2, p3) => userDelegate(p1, p2, p3);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3, T4>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, T4, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, T4, Response>> delegExpr = (p1, p2, p3, p4) => userDelegate(p1, p2, p3, p4);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3, T4, T5>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, T4, T5, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, T4, T5, Response>> delegExpr = (p1, p2, p3, p4, p5) => userDelegate(p1, p2, p3, p4, p5);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3, T4, T5, T6>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, T4, T5, T6, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, T4, T5, T6, Response>> delegExpr = (p1, p2, p3, p4, p5, p6) => userDelegate(p1, p2, p3, p4, p5, p6);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3, T4, T5, T6, T7>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, T4, T5, T6, T7, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, T4, T5, T6, T7, Response>> delegExpr = (p1, p2, p3, p4, p5, p6, p7) => userDelegate(p1, p2, p3, p4, p5, p6, p7);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        public Func<Request, IntPtr, IntPtr, Response> GenerateParsingDelegate<T1, T2, T3, T4, T5, T6, T7, T8>(
            UInt16 port,
            String methodAndUriInfo,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, Response> userDelegate,
            HandlerOptions ho,
            MixedCodeConstants.NetworkProtocolType protoType = MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            if (!userDelegate.Method.IsStatic)
            {
                Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, Response>> delegExpr = (p1, p2, p3, p4, p5, p6, p7, p8) => userDelegate(p1, p2, p3, p4, p5, p6, p7, p8);
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, delegExpr, protoType, ho);
            }
            else
            {
                return RegisterDelegate(port, methodAndUriInfo, userDelegate.Method, null, protoType, ho);
            }
        }

        static UriManagedHandlersCodegen user_codegen_handler_ = new UriManagedHandlersCodegen();

        public static UriManagedHandlersCodegen UMHC
        {
            get { return user_codegen_handler_; }
        }

        public static void Setup(
            UriInjectMethods.RegisterUriHandlerNativeDelegate registerUriHandlerNative,
            TcpSocket.RegisterTcpSocketHandlerDelegate tcpSocketHandler,
            UdpSocket.RegisterUdpSocketHandlerDelegate udpSocketHandler,
            Func<Request, Boolean> onHttpMessageRoot,
            Func<Request, HandlerOptions, Response> runDelegateAndProcessResponse)
        {
            TcpSocket.InitTcpSockets(tcpSocketHandler);
            UdpSocket.InitUdpSockets(udpSocketHandler);

            UriInjectMethods.SetDelegates(
                registerUriHandlerNative,
                onHttpMessageRoot,
                runDelegateAndProcessResponse);

            RequestHandler.InitREST();
            UriHandlersManager.Init();

            // Setting the pointer to get current session.
            ScSessionClass.GetCurrent = () => {
                if (Session.Current != null)
                    return Session.Current.InternalSession;

                return null;
            };

        }

        /// <summary>
        /// Run URI matcher and call determined handler.
        /// </summary>
        internal static Boolean RunUriMatcherAndCallHandler(
            String methodSpaceUriSpace,
            Byte[] requestBytes,
            Int32 requestBytesLength,
            UInt16 portNumber,
            HandlerOptions handlerOptions,
            out Response resp)
        {
            resp = null;

            // Checking if local RESTing is initialized.
            if (!UriInjectMethods.IsSupportingLocalNodeResting()) {
                return false;
            }

            // Getting appropriate handler level manager.
            UriHandlersManager uhm = UriHandlersManager.GetUriHandlersManager(handlerOptions.HandlerLevel);

            // Checking if there are any registered handlers.
            if (uhm.NumRegisteredHandlers <= 0) {
                return false;
            }

            // Checking if port is initialized.
            PortUris portUris = uhm.SearchPort(portNumber);
            if (portUris == null)
                portUris = uhm.AddPort(portNumber);

            // Calling the code generation for URIs if needed.
            if (null == portUris.MatchUriAndGetHandlerId) {

                if (!portUris.GenerateUriMatcher(
                    portNumber,
                    uhm.AllUserHandlerInfos,
                    uhm.NumRegisteredHandlers)) {

                    return false;
                }
            }

            // Calling the generated URI matcher.
            Int32 handlerId = handlerOptions.HandlerId;

            unsafe {

                // Allocating space for parameter information.
                Byte* paramsStackBuf = stackalloc Byte[MixedCodeConstants.PARAMS_INFO_MAX_SIZE_BYTES];

                MixedCodeConstants.UserDelegateParamInfo* handlerNativeParams = (MixedCodeConstants.UserDelegateParamInfo*) paramsStackBuf;

                // Setting parameters info from handler options.
                *handlerNativeParams = handlerOptions.ParametersInfo;

                // Checking if handler is predefined.
                if (HandlerOptions.InvalidUriHandlerId == handlerId) {

                    // Pointing to address of handler parameters.
                    MixedCodeConstants.UserDelegateParamInfo** handlerNativeParamsAddr = &handlerNativeParams;

                    // Copying string to stack buffer instead of pinning the request bytes.
                    Int32 len = methodSpaceUriSpace.Length;
                    Byte* methodSpaceUriSpaceOnStack = stackalloc Byte[len];
                    for (Int32 i = 0; i < len; i++) {
                        methodSpaceUriSpaceOnStack[i] = (Byte) methodSpaceUriSpace[i];
                    }

                    // TODO: Resolve this hack with only positive handler ids in generated code.
                    handlerId = portUris.MatchUriAndGetHandlerId(methodSpaceUriSpaceOnStack, (UInt32)len, handlerNativeParamsAddr) - 1;
                }

                // Checking if we have found the handler.
                if (handlerId >= 0) {

                    // Creating HTTP request.
                    Request req = new Request(requestBytes, requestBytesLength, paramsStackBuf);

                    req.ManagedHandlerId = (UInt16) handlerId;
                    req.MethodEnum = uhm.AllUserHandlerInfos[handlerId].UriInfo.http_method_;

                    // Invoking original user delegate with parameters here.
                    resp = UriInjectMethods.runDelegateAndProcessResponse_(req, handlerOptions);

                    // Checking if handled the response.
                    if (resp == null)
                        return false;

                    // Parsing the response.
                    resp.ParseResponseFromPlainBuffer();

                    // Request successfully handled.
                    return true;
                }
            }

            return false;
        }
    }
}
