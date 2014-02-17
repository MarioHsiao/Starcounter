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

        Int32 channelId_;

        public Int32 ChannelId { get { return channelId_; } }

        public WsChannelInfo(String channel)
        {
            Channel = channel;
            channelId_ = channel.GetHashCode();
        }

        public void SetReceiveBinaryHandler(Action<Byte[], WebSocket> receiveBinaryHandler)
        {
            receiveBinaryHandler_ = receiveBinaryHandler;
        }

        public void SetReceiveStringHandler(Action<String, WebSocket> receiveStringHandler)
        {
            receiveStringHandler_ = receiveStringHandler;
        }

        public void SetDisconnectHandler(Action<WebSocket> disconnectHandler)
        {
            disconnectHandler_ = disconnectHandler;
        }

        public void DetermineAndRunHandler(WebSocket ws)
        {
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

    internal class PortWsChannels
    {
        List<WsChannelInfo> portWsChannels_ = new List<WsChannelInfo>();

        UInt16 port_;

        public UInt16 Port
        {
            get { return port_; }
        }

        public PortWsChannels(UInt16 port)
        {
            port_ = port;
        }

        public WsChannelInfo FindChannel(String channel)
        {
            foreach (WsChannelInfo p in portWsChannels_)
            {
                if (0 == String.Compare(p.Channel, channel, StringComparison.InvariantCultureIgnoreCase))
                    return p;
            }
            return null;
        }

        public WsChannelInfo FindChannelById(Int32 channelId)
        {
            foreach (WsChannelInfo p in portWsChannels_)
            {
                if (p.ChannelId == channelId)
                    return p;
            }
            return null;
        }

        public WsChannelInfo CreateNewChannel(String channel)
        {
            WsChannelInfo p = new WsChannelInfo(channel);
            return p;
        }
    }

    internal class AllWsChannels
    {
        static AllWsChannels wsManager_ = new AllWsChannels();

        delegate void RegisterWsHandler(
            UInt16 port,
            String channelName,
            UInt32 channelId,
            HandlersManagement.UriCallbackDelegate wsCallback,
            out UInt16 handlerId,
            out Int32 maxNumEntries);

        public static AllWsChannels WsChannels
        {
            get { return wsManager_; }
        }

        List<PortWsChannels> allWsChannels_ = new List<PortWsChannels>();

        PortWsChannels SearchPort(UInt16 port)
        {
            foreach (PortWsChannels pu in allWsChannels_)
            {
                if (pu.Port == port)
                    return pu;
            }
            return null;
        }

        PortWsChannels CreatePortWsChannels(UInt16 port)
        {
            // Searching for existing port if any.
            PortWsChannels p = SearchPort(port);
            if (p != null)
                return p;

            // Adding new port entry.
            p = new PortWsChannels(port);
            allWsChannels_.Add(p);
            return p;
        }

        WsChannelInfo CreateOrFindWsChannel(UInt16 port, String channel)
        {
            PortWsChannels p = SearchPort(port);
            if (p == null)
            {
                p = CreatePortWsChannels(port);
                allWsChannels_.Add(p);
            }

            WsChannelInfo w = p.FindChannel(channel);
            if (w == null)
                w = p.CreateNewChannel(channel);

            return w;
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channel,
            Action<Byte[], WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = CreateOrFindWsChannel(port, channel);

                w.SetReceiveBinaryHandler(userDelegate);
            }
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String channel,
            Action<String, WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = CreateOrFindWsChannel(port, channel);

                w.SetReceiveStringHandler(userDelegate);
            }
        }

        public void RegisterWsDisconnectDelegate(
            UInt16 port,
            String channel,
            Action<WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsChannelInfo w = CreateOrFindWsChannel(port, channel);

                w.SetDisconnectHandler(userDelegate);
            }
        }

        public void RunHandler(UInt16 port, Int32 channelId, WebSocket ws)
        {
            PortWsChannels p = SearchPort(port);
            if (p != null)
            {
                WsChannelInfo w = p.FindChannelById(channelId);
                if (w != null)
                {
                    w.DetermineAndRunHandler(ws);
                }
            }
        }
    }
}