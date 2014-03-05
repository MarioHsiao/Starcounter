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

        public String ChannelName { get; set; }

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

        public WsChannelInfo(UInt16 handlerId, UInt16 port, String channel)
        {
            ChannelName = channel;
            channelId_ = (UInt32)channel.GetHashCode();
            handlerId_ = handlerId;
            port_ = port;
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

        public WsChannelInfo FindChannel(String channelName)
        {
            // Pre-pending database name for automatic uniqueness.
            channelName = StarcounterEnvironment.DatabaseNameLower + channelName;

            for (Int32 i = 0; i < maxWsChannels_; i++)
            {
                if ((allWsChannels_[i] != null) && (allWsChannels_[i].Alive))
                {
                    if (0 == String.Compare(allWsChannels_[i].ChannelName, channelName))
                        return allWsChannels_[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Registers the WebSocket handler.
        /// </summary>
        void RegisterWsHandlerBmx(
            UInt16 port,
            String channelName,
            UInt32 channelId,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo)
        {
            // Ensuring correct multi-threading handlers creation.
            unsafe
            {
                UInt32 errorCode = bmx.sc_bmx_register_ws_handler(
                    port,
                    channelName,
                    channelId,
                    WebsocketOuterHandler_,
                    managedHandlerIndex,
                    out handlerInfo);

                if (errorCode != 0)
                    throw ErrorCode.ToException(errorCode, "Channel string: " + channelName);
            }
        }

        static AllWsChannels wsManager_ = new AllWsChannels();

        public static AllWsChannels WsManager { get { return wsManager_; } }

        bmx.BMX_HANDLER_CALLBACK WebsocketOuterHandler_;

        internal void InitWebSockets(bmx.BMX_HANDLER_CALLBACK WebsocketOuterHandler) {
            WebsocketOuterHandler_ = WebsocketOuterHandler;
            WebSocket.InitWebSocketsInternal();
        }

        WsChannelInfo CreateWsChannel(UInt16 port, String channelName)
        {
            if (maxWsChannels_ >= MAX_WS_HANDLERS)
                throw ErrorCode.ToException(Error.SCERRMAXHANDLERSREACHED);

            // Not found, creating new.
            WsChannelInfo w = new WsChannelInfo(maxWsChannels_, port, channelName);
            allWsChannels_[maxWsChannels_] = w;

            maxWsChannels_++;

            return w;
        }

        WsChannelInfo FindWsChannel(UInt16 port, String channelName)
        {
            // Searching WebSocket channel.
            for (UInt16 i = 0; i < maxWsChannels_; i++)
            {
                if ((allWsChannels_[i] != null) && (allWsChannels_[i].Alive))
                {
                    if ((allWsChannels_[i].Port == port) && (allWsChannels_[i].ChannelName == channelName))
                        return allWsChannels_[i];
                }
            }

            return null;
        }

        WsChannelInfo RegisterHandlerInternal(UInt16 port, String channelName)
        {
            if (channelName.Length > 32)
                throw new Exception("Registering too long channel name: " + channelName);

            // Pre-pending database name for automatic uniqueness.
            channelName = StarcounterEnvironment.DatabaseNameLower + channelName;

            WsChannelInfo w = FindWsChannel(port, channelName);

            if (w == null)
            {
                w = CreateWsChannel(port, channelName);

                UInt64 handlerInfo;
                RegisterWsHandlerBmx(port, channelName, w.ChannelId, w.HandlerId, out handlerInfo);
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