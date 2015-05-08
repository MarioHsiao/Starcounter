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
    internal class WsGroupInfo
    {
        Action<Byte[], WebSocket> receiveBinaryHandler_;

        Action<String, WebSocket> receiveStringHandler_;

        Action<WebSocket> disconnectHandler_;

        public String InternalGroupName { get; set; }

        UInt16 port_;

        public UInt16 Port { get { return port_; } }

        UInt32 groupId_;

        UInt16 handlerId_;

        public UInt16 HandlerId { get { return handlerId_; } }

        UInt64 handlerInfo_;

        public UInt64 HandlerInfo {
            get { return handlerInfo_; }
            set { handlerInfo_ = value; }
        }

        public UInt32 GroupId { get { return groupId_; } }

        Boolean alive_;

        public Boolean Alive { get { return alive_; } }

        String appName_;

        public String AppName { get { return appName_; } }

        public WsGroupInfo(String appName, UInt16 handlerId, UInt16 port, String internalGroupName)
        {
            InternalGroupName = internalGroupName;
            groupId_ = CalculateGroupIdFromInternalName(internalGroupName);
            handlerId_ = handlerId;
            port_ = port;
            appName_ = appName;
        }

        static UInt32 CalculateGroupIdFromInternalName(String internalGroupName) {
            return (UInt32)internalGroupName.GetHashCode();
        }

        public static UInt32 CalculateGroupIdFromGroupName(String groupName) {
            return CalculateGroupIdFromInternalName(StarcounterEnvironment.DatabaseNameLower + groupName);
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
                    if (disconnectHandler_ != null) {
                        disconnectHandler_(ws);
                    }

                    break;
                }

                default:
                    throw new Exception("Unknown WebSocket handler is attempted to run!");
            }
        }
    }

    internal class AllWsGroups
    {
        public const Int32 MAX_WS_HANDLERS = 512;

        WsGroupInfo[] allWsGroups_ = new WsGroupInfo[MAX_WS_HANDLERS];

        UInt16 maxWsGroups_ = 0;

        internal WsGroupInfo FindGroup(UInt16 port, String groupName)
        {
            // Pre-pending database name for automatic uniqueness.
            groupName = StarcounterEnvironment.DatabaseNameLower + groupName;

            // Searching WebSocket channel.
            for (UInt16 i = 0; i < maxWsGroups_; i++)
            {
                if ((allWsGroups_[i] != null) && (allWsGroups_[i].Alive))
                {
                    if ((allWsGroups_[i].Port == port) && (0 == String.Compare(allWsGroups_[i].InternalGroupName, groupName))) {
                        return allWsGroups_[i];
                    }
                }
            }

            return null;
        }

        static AllWsGroups wsManager_ = new AllWsGroups();

        public static AllWsGroups WsManager { get { return wsManager_; } }

        internal delegate void RegisterWsChannelHandlerNativeDelegate(
            UInt16 port,
            String appName,
            String groupName,
            UInt32 groupId,
            UInt16 managedHandlerIndex,
            out UInt64 handlerInfo);

        RegisterWsChannelHandlerNativeDelegate RegisterWsGroupHandlerNative_;

        internal void InitWebSockets(RegisterWsChannelHandlerNativeDelegate registerWsGroupHandlerNative) {
            RegisterWsGroupHandlerNative_ = registerWsGroupHandlerNative;
        }

        WsGroupInfo CreateWsGroup(UInt16 port, String internalGroupName)
        {
            if (maxWsGroups_ >= MAX_WS_HANDLERS) {
                throw ErrorCode.ToException(Error.SCERRMAXHANDLERSREACHED);
            }

            // Not found, creating new.
            WsGroupInfo w = new WsGroupInfo(StarcounterEnvironment.AppName, maxWsGroups_, port, internalGroupName);
            allWsGroups_[maxWsGroups_] = w;

            maxWsGroups_++;

            return w;
        }

        WsGroupInfo RegisterHandlerInternal(UInt16 port, String groupName)
        {
            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    port = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    port = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            if (groupName.Length > 32) {
                throw new Exception("Registering too long group name: " + groupName);
            }

            WsGroupInfo group = FindGroup(port, groupName);

            // Pre-pending database name for automatic uniqueness.
            String internalGroupName = StarcounterEnvironment.DatabaseNameLower + groupName;

            // Checking if group is found.
            if (group == null) {

                group = CreateWsGroup(port, internalGroupName);

                String appName = group.AppName;
                if (String.IsNullOrEmpty(appName)) {
                    appName = MixedCodeConstants.EmptyAppName;
                }

                UInt64 handlerInfo;
                RegisterWsGroupHandlerNative_(port, appName, internalGroupName, group.GroupId, group.HandlerId, out handlerInfo);
                group.HandlerInfo = handlerInfo;
            }

            return group;
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String groupName,
            Action<Byte[], WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsGroupInfo w = RegisterHandlerInternal(port, groupName);

                w.SetReceiveBinaryHandler(userDelegate);
            }
        }

        public void RegisterWsDelegate(
            UInt16 port,
            String groupName,
            Action<String, WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsGroupInfo w = RegisterHandlerInternal(port, groupName);

                w.SetReceiveStringHandler(userDelegate);
            }
        }

        public void RegisterWsDisconnectDelegate(
            UInt16 port,
            String groupName,
            Action<WebSocket> userDelegate)
        {
            lock (wsManager_)
            {
                WsGroupInfo w = RegisterHandlerInternal(port, groupName); 

                w.SetDisconnectHandler(userDelegate);
            }
        }

        public Boolean RunHandler(UInt16 handlerid, UInt32 groupId, WebSocket ws)
        {
            if ((allWsGroups_[handlerid] != null) && 
                (allWsGroups_[handlerid].Alive) &&
                (allWsGroups_[handlerid].GroupId == groupId)) {

                allWsGroups_[handlerid].DetermineAndRunHandler(ws);
                return true;

            } else {

                ws.Disconnect("WebSocket handlers on the requested channel are not registered. Closing the connection.",
                    WebSocket.WebSocketCloseCodes.WS_CLOSE_CANT_ACCEPT_DATA);
            }

            return false;
        }
    }
}