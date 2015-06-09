using System.Threading.Tasks;
using Starcounter.Rest;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Advanced;
using System.Text;
using System.Runtime.InteropServices;

namespace Starcounter.Internal
{
    /// <summary>
    /// Generic object finalizer.
    /// </summary>
    class Finalizer {

        /// <summary>
        /// Object that should be finalized.
        /// </summary>
        Finalizing obj_;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="obj"></param>
        internal Finalizer(Finalizing obj) {
            obj_ = obj;
        }

        internal void UnLink() {
            obj_ = null;
        }

        ~Finalizer() {

            if (null != obj_) {
                obj_.DestroyByFinalizer();
                obj_ = null;
            }
        }
    }

    /// <summary>
    /// Finalizing class.
    /// </summary>
    public abstract class Finalizing {

        /// <summary>
        /// Reference to finalizer object.
        /// </summary>
        Finalizer finalizer_;

        /// <summary>
        /// Destroy by finalizer.
        /// </summary>
        abstract internal void DestroyByFinalizer();

        /// <summary>
        /// Creates finalizer.
        /// </summary>
        internal void CreateFinalizer() {

            if (null == finalizer_) {
                finalizer_ = new Finalizer(this);
            }
        }

        /// <summary>
        /// Has finalizer attached?
        /// </summary>
        /// <returns></returns>
        internal Boolean HasFinalizer() {
            return (null != finalizer_);
        }

        /// <summary>
        /// Unlinking finalizer.
        /// </summary>
        internal void UnLinkFinalizer() {

            // NOTE: Removing reference for finalizer so it does not call destroy again.
            if (null != finalizer_) {
                finalizer_.UnLink();
                finalizer_ = null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SocketId {
        public UInt64 IdLower;
        public UInt64 IdUpper;
    }

    /// <summary>
    /// Socket structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SocketStruct {

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        UInt64 socketUniqueId_;

        /// <summary>
        /// Unique socket id on gateway.
        /// </summary>
        internal UInt64 SocketUniqueId {
            get {
                return socketUniqueId_;
            }
        }

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        UInt32 socketIndexNum_;

        /// <summary>
        /// Socket index on gateway.
        /// </summary>
        internal UInt32 SocketIndexNum {
            get {
                return socketIndexNum_;
            }
        }

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        Byte gatewayWorkerId_;

        /// <summary>
        /// Gateway worker id.
        /// </summary>
        internal Byte GatewayWorkerId {
            get {
                return gatewayWorkerId_;
            }
        }

        /// <summary>
        /// Initializes socket struct.
        /// </summary>
        public void Init(
            UInt32 socketIndexNum,
            UInt64 socketUniqueId,
            Byte gatewayWorkerId) {

            socketUniqueId_ = socketUniqueId;
            socketIndexNum_ = socketIndexNum;
            gatewayWorkerId_ = gatewayWorkerId;
        }

        /// <summary>
        /// Invalidates socket struct.
        /// </summary>
        public void Kill() {

            socketUniqueId_ = 0;
        }

        /// <summary>
        /// Checks if socket struct is dead.
        /// </summary>
        public Boolean IsDead() {

            return socketUniqueId_ == 0;
        }

        /// <summary>
        /// Creates socket struct from lower and upper parts.
        /// </summary>
        public static SocketStruct FromLowerUpper(
            UInt64 socketIdLower, UInt64 socketIdUpper) {

            unsafe {

                SocketId socketId = new SocketId() {
                    IdLower = socketIdLower,
                    IdUpper = socketIdUpper
                };

                SocketStruct s = *(SocketStruct*)(&socketId);

                return s;
            }
        }

        /// <summary>
        /// Convert socket struct to lower and upper parts.
        /// </summary>
        public static void ToLowerUpper(
            SocketStruct s,
            out UInt64 idLower,
            out UInt64 idUpper) {

            unsafe {

                SocketId socketId = *(SocketId*)(&s);

                idLower = socketId.IdLower;
                idUpper = socketId.IdUpper;
            }
        }

        /// <summary>
        /// Converts socket struct to lower and upper parts.
        /// </summary>
        public UInt64 ToUInt64() {

            UInt64 ws = socketUniqueId_ | (((UInt64)socketIndexNum_) << 40) | (((UInt64)gatewayWorkerId_) << 60);

            return ws;
        }

        /// <summary>
        /// Converts socket struct to lower and upper parts.
        /// </summary>
        public void FromUInt64(UInt64 ws) {

            socketUniqueId_ = ws & 0xFFFFFFFFFF;
            socketIndexNum_ = (UInt32) ((ws >> 40) & 0xFFFFF);
            gatewayWorkerId_ = (Byte)((ws >> 60) & 0xF);
        }

        /// <summary>
        /// Converts a string to socket.
        /// </summary>
        public void FromString(String s) {

            Byte[] strBytes = Encoding.ASCII.GetBytes(s);

            socketUniqueId_ = (UInt64)ScSessionStruct.hex_string_to_uint64(strBytes, 0, 16);
            socketIndexNum_ = (UInt32)ScSessionStruct.hex_string_to_uint64(strBytes, 16, 6);
            gatewayWorkerId_ = (Byte)ScSessionStruct.hex_string_to_uint64(strBytes, 22, 2);
        }

        /// <summary>
        /// Serializing session structure to bytes.
        /// </summary>
        public void SerializeToBytes(Byte[] session_bytes) {

            ScSessionStruct.uint64_to_hex_string(socketUniqueId_, session_bytes, 0, 16);
            ScSessionStruct.uint64_to_hex_string(socketIndexNum_, session_bytes, 16, 6);
            ScSessionStruct.uint64_to_hex_string(gatewayWorkerId_, session_bytes, 22, 2);
        }

        public const Int32 SocketStructSize = 14;

        /// <summary>
        /// Converts socket struct to string.
        /// </summary>
        /// <returns></returns>
        public String GetString() {
            Byte[] tempArray = new Byte[SocketStructSize];

            SerializeToBytes(tempArray);

            return Encoding.ASCII.GetString(tempArray);
        }
    }

    public class SchedulerResources {

        public Response AggregationStubResponse = new Response() { Body = "Xaxa!" };

        static SchedulerResources[] all_schedulers_resources_;

        public static void Init(Int32 numSchedulers) {
            all_schedulers_resources_ = new SchedulerResources[numSchedulers];

            for (Int32 i = 0; i < numSchedulers; i++) {
                all_schedulers_resources_[i] = new SchedulerResources();
                all_schedulers_resources_[i].AggregationStubResponse.ConstructFromFields(null, null);
            }
        }

        public static SchedulerResources Current {
            get { return all_schedulers_resources_[StarcounterEnvironment.CurrentSchedulerId]; }
        }
    }
}