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
using System.IO;
using System.Globalization;
using Starcounter.Rest;

namespace Starcounter.Internal
{
    internal class WsChannelInfo
    {
        Action<Byte[], WebSocket> receiveBinaryHandler_;

        Action<String, WebSocket> receiveStringHandler_;

        Action<UInt64, IAppsSession> disconnectHandler_;

        public String InternalChannelName { get; set; }

        UInt16 port_;

        public UInt16 Port { get { return port_; } }

        UInt32 channelId_;

        UInt16 handlerId_;

        public UInt16 HandlerId { get { return handlerId_; } }

        UInt64 handlerInfo_;

        public UInt64 HandlerInfo {
            get { return handlerInfo_; }
            set { handlerInfo_ = value; }
        }

        public UInt32 ChannelId { get { return channelId_; } }

        Boolean alive_;

        public Boolean Alive { get { return alive_; } }

        String appName_;

        public String AppName { get { return appName_; } }

        public WsChannelInfo(String appName, UInt16 handlerId, UInt16 port, String internalChannelName)
        {
            InternalChannelName = internalChannelName;
            channelId_ = CalculateChannelIdFromInternalName(internalChannelName);
            handlerId_ = handlerId;
            port_ = port;
            appName_ = appName;
        }

        static UInt32 CalculateChannelIdFromInternalName(String internalChannelName) {
            return (UInt32)internalChannelName.GetHashCode();
        }

        public static UInt32 CalculateChannelIdFromChannelName(String channelName) {
            return CalculateChannelIdFromInternalName(StarcounterEnvironment.DatabaseNameLower + channelName);
        }

        public void Destroy()
        {
            alive_ = false;
        }

        public void SetReceiveBinaryHandler(Action<Byte[], WebSocket> receiveBinaryHandler)
        {
            receiveBinaryHandler_ = receiveBinaryHandler;
            alive_ = true;
        }

        public void SetReceiveStringHandler(Action<String, WebSocket> receiveStringHandler)
        {
            receiveStringHandler_ = receiveStringHandler;
            alive_ = true;
        }

        public void SetDisconnectHandler(Action<UInt64, IAppsSession> disconnectHandler)
        {
            disconnectHandler_ = disconnectHandler;
            alive_ = true;
        }

        public void DetermineAndRunHandler(WebSocket ws)
        {
            StarcounterEnvironment.AppName = appName_;

            switch (ws.HandlerType)
            {
                case WebSocket.WsHandlerType.BinaryData:
                {
                    if (receiveBinaryHandler_ == null)
                    {
                        ws.Disconnect("WebSocket binary messages handler on the requested channel is not registered. Closing the connection.",
                            WebSocket.WebSocketCloseCodes.WS_CLOSE_CANT_ACCEPT_DATA);

                        return;
                    }

                    receiveBinaryHandler_(ws.Bytes, ws);

                    break;
                }

                case WebSocket.WsHandlerType.StringMessage:
                {
                    if (receiveStringHandler_ == null)
                    {
                        ws.Disconnect("WebSocket string messages handler on the requested channel is not registered. Closing the connection.",
                            WebSocket.WebSocketCloseCodes.WS_CLOSE_CANT_ACCEPT_DATA);

                        return;
                    }

                    receiveStringHandler_(ws.Message, ws);

                    break;
                }

                case WebSocket.WsHandlerType.Disconnect:
                {
                    if (disconnectHandler_ != null)
                        disconnectHandler_(ws.CargoId, ws.Session);

                    ws.Destroy(true);

                    break;
                }

                default:
                    throw new Exception("Unknown WebSocket handler is attempted to run!");
            }
        }
    }

    internal class AllWsChannels
    {
        public const Int32 MAX_WS_HANDLERS = 512;

        WsChannelInfo[] allWsChannels_ = new WsChannelInfo[MAX_WS_HANDLERS];

        UInt16 maxWsChannels_ = 0;

        internal WsChannelInfo FindChannel(UInt16 port, String channelName)
        {
            // Pre-pending database name for automatic uniqueness.
            channelName = StarcounterEnvironment.DatabaseNameLower + channelName;

            // Searching WebSocket channel.
            for (UInt16 i = 0; i < maxWsChannels_; i++)
            {
                if ((allWsChannels_[i] != null) && (allWsChannels_[i].Alive))
                {
                    if ((allWsChannels_[i].Port == port) && (0 == String.Compare(allWsChannels_[i].InternalChannelName, channelName)))
                        return allWsChannels_[i];
                }
            }

            return null;
        }

        static AllWsChannels wsManager_ = new AllWsChannels();

        public static AllWsChannels WsManager { get { return wsManager_; } }

        internal delegate void RegisterWsChannelHandlerNativeDelegate(
            UInt16 port,
            String appName,
            String channelName,
            UInt32 channelId,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo);

        RegisterWsChannelHandlerNativeDelegate RegisterWsChannelHandlerNative_;

        internal void InitWebSockets(RegisterWsChannelHandlerNativeDelegate registerWsChannelHandlerNative) {
            RegisterWsChannelHandlerNative_ = registerWsChannelHandlerNative;
            SchedulerResources.InitSockets();
        }

        WsChannelInfo CreateWsChannel(UInt16 port, String internalChannelName)
        {
            if (maxWsChannels_ >= MAX_WS_HANDLERS)
                throw ErrorCode.ToException(Error.SCERRMAXHANDLERSREACHED);

            // Not found, creating new.
            WsChannelInfo w = new WsChannelInfo(StarcounterEnvironment.AppName, maxWsChannels_, port, internalChannelName);
            allWsChannels_[maxWsChannels_] = w;

            maxWsChannels_++;

            return w;
        }

        WsChannelInfo RegisterHandlerInternal(UInt16 port, String channelName)
        {
            if (channelName.Length > 32)
                throw new Exception("Registering too long channel name: " + channelName);

            WsChannelInfo w = FindChannel(port, channelName);

            // Pre-pending database name for automatic uniqueness.
            String internalChannelName = StarcounterEnvironment.DatabaseNameLower + channelName;

            if (w == null)
            {
                w = CreateWsChannel(port, internalChannelName);

                String appName = w.AppName;
                if (String.IsNullOrEmpty(appName)) {
                    appName = MixedCodeConstants.EmptyAppName;
                }

                UInt64 handlerInfo;
                RegisterWsChannelHandlerNative_(port, appName, internalChannelName, w.ChannelId, w.HandlerId, out handlerInfo);
                w.HandlerInfo = handlerInfo;
            }

            return w;
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channelName,
            Action<Byte[], WebSocket> userDelegate)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandlerInternal(port, channelName);

                w.SetReceiveBinaryHandler(userDelegate);
            }
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channelName,
            Action<String, WebSocket> userDelegate)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandlerInternal(port, channelName);

                w.SetReceiveStringHandler(userDelegate);
            }
        }

        public void RegisterWsDisconnectDelegate(
            UInt16 port,
            String channelName,
            Action<UInt64, IAppsSession> userDelegate)
        {
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port)
                port = StarcounterEnvironment.Default.UserHttpPort;

            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandlerInternal(port, channelName); 

                w.SetDisconnectHandler(userDelegate);
            }
        }

        public Boolean RunHandler(UInt16 id, WebSocket ws)
        {
            if (allWsChannels_[id] != null && allWsChannels_[id].Alive)
            {
                allWsChannels_[id].DetermineAndRunHandler(ws);
                return true;
            }
            else
            {
                ws.Disconnect("WebSocket handlers on the requested channel are not registered. Closing the connection.",
                    WebSocket.WebSocketCloseCodes.WS_CLOSE_CANT_ACCEPT_DATA);
            }

            return false;
        }
    }
}