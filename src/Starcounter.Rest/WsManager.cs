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
using Starcounter.Rest;

namespace Starcounter.Internal
{
    internal class WsChannelInfo
    {
        Action<Byte[], WebSocket> receiveBinaryHandler_;

        Action<String, WebSocket> receiveStringHandler_;

        Action<WebSocket> disconnectHandler_;

        public String Channel { get; set; }

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
            Channel = channel;
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

        public void SetDisconnectHandler(Action<WebSocket> disconnectHandler)
        {
            disconnectHandler_ = disconnectHandler;
            alive_ = true;
        }

        public void DetermineAndRunHandler(UInt64 handlerInfo, WebSocket ws)
        {
            if (handlerInfo != handlerInfo_)
                return;

            if (ws.Message != null)
            {
                if (receiveStringHandler_ != null)
                    receiveStringHandler_(ws.Message, ws);
            }
            else if (ws.Bytes != null)
            {
                if (receiveBinaryHandler_ != null)
                    receiveBinaryHandler_(ws.Bytes, ws);
            }
            else
            {
                if (disconnectHandler_ != null)
                    disconnectHandler_(ws);
            }
        }
    }

    internal class AllWsChannels
    {
        WsChannelInfo[] allWsChannels_ = new WsChannelInfo[HandlersManagement.MAX_USER_HANDLERS];

        Int32 maxWsChannels_ = 0;

        public WsChannelInfo FindChannel(String channel)
        {
            foreach (WsChannelInfo p in allWsChannels_)
            {
                if (0 == String.Compare(p.Channel, channel, StringComparison.InvariantCultureIgnoreCase))
                    return p;
            }
            return null;
        }

        public WsChannelInfo FindChannelById(Int32 channelId)
        {
            foreach (WsChannelInfo p in allWsChannels_)
            {
                if (p.ChannelId == channelId)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Registers the WebSocket handler.
        /// </summary>
        void RegisterWsHandler(
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

        internal void SetWebsocketOuterHandler(bmx.BMX_HANDLER_CALLBACK WebsocketOuterHandler) {
            WebsocketOuterHandler_ = WebsocketOuterHandler;
        }

        WsChannelInfo CreateOrFindWsChannel(UInt16 port, String channelName)
        {
            if (maxWsChannels_ >= HandlersManagement.MAX_USER_HANDLERS)
                throw ErrorCode.ToException(Error.SCERRMAXHANDLERSREACHED);

            // Searching WebSocket channel.
            UInt16 i;
            for (i = 0; i < maxWsChannels_; i++)
            {
                if ((allWsChannels_[i] != null) && (allWsChannels_[i].Alive))
                {
                    if ((allWsChannels_[i].Port == port) && (allWsChannels_[i].Channel == channelName))
                        return allWsChannels_[i];
                }
            }

            if (i == maxWsChannels_)
                maxWsChannels_++;

            // Not found, creating new.
            WsChannelInfo w = new WsChannelInfo(i, port, channelName);
            allWsChannels_[i] = w;

            return w;
        }

        WsChannelInfo RegisterHandler(UInt16 port, String channelName)
        {
            WsChannelInfo w = CreateOrFindWsChannel(port, channelName);
            UInt64 handlerInfo;
            RegisterWsHandler(port, channelName, w.ChannelId, w.HandlerId, out handlerInfo);
            w.HandlerInfo = handlerInfo;

            return w;
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channelName,
            Action<Byte[], WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandler(port, channelName);

                w.SetReceiveBinaryHandler(userDelegate);
            }
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channelName,
            Action<String, WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandler(port, channelName);

                w.SetReceiveStringHandler(userDelegate);
            }
        }

        public void RegisterWsDisconnectDelegate(
            UInt16 port,
            String channelName,
            Action<WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = RegisterHandler(port, channelName); 

                w.SetDisconnectHandler(userDelegate);
            }
        }

        public void RunHandler(UInt16 id, UInt64 handlerInfo, WebSocket ws)
        {
            if (allWsChannels_[id] != null && allWsChannels_[id].Alive)
                allWsChannels_[id].DetermineAndRunHandler(handlerInfo, ws);
        }
    }
}