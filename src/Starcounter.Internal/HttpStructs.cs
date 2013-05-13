// ***********************************************************************
// <copyright file="HttpStructs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpStructs
{
    /// <summary>
    /// Enum HTTP_METHODS
    /// </summary>
    public enum HTTP_METHODS
    {
        /// <summary>
        /// The GE t_ METHOD
        /// </summary>
        GET_METHOD,
        /// <summary>
        /// The POS t_ METHOD
        /// </summary>
        POST_METHOD,
        /// <summary>
        /// The PU t_ METHOD
        /// </summary>
        PUT_METHOD,
        /// <summary>
        /// The DELET e_ METHOD
        /// </summary>
        DELETE_METHOD,
        /// <summary>
        /// The HEA d_ METHOD
        /// </summary>
        HEAD_METHOD,
        /// <summary>
        /// The OPTION s_ METHOD
        /// </summary>
        OPTIONS_METHOD,
        /// <summary>
        /// The TRAC e_ METHOD
        /// </summary>
        TRACE_METHOD,
        /// <summary>
        /// The PATC h_ METHOD
        /// </summary>
        PATCH_METHOD,
        /// <summary>
        /// The OTHE r_ METHOD
        /// </summary>
        OTHER_METHOD
    };

    /// <summary>
    /// Interface INetworkDataStream
    /// </summary>
    public interface INetworkDataStream
    {
        /// <summary>
        /// Inits the specified unmanaged chunk.
        /// </summary>
        /// <param name="unmanagedChunk">The unmanaged chunk.</param>
        /// <param name="isSingleChunk">The is single chunk.</param>
        /// <param name="chunkIndex">Index of the chunk.</param>
        unsafe void Init(Byte* unmanagedChunk, Boolean isSingleChunk, UInt32 chunkIndex);

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        void Read(Byte[] buffer, Int32 offset, Int32 length);

        /// <summary>
        /// Send the specified buffer as a response.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        void SendResponse(Byte[] buffer, Int32 offset, Int32 length);

        /// <summary>
        /// Frees all data stream resources like chunks.
        /// </summary>
        void Destroy();
    }

    /// <summary>
    /// Struct ScSessionStruct
    /// </summary>
    public struct ScSessionStruct
    {
        // Scheduler id.
        public Byte scheduler_id_;

        // Session linear index.
        public UInt32 linear_index_;

        // Unique random salt.
        public UInt64 random_salt_;

        // View model number.
        public UInt32 view_model_index_;

        public void Init(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt,
            UInt32 view_model_index)
        {
            scheduler_id_ = scheduler_id;
            linear_index_ = linear_index;
            random_salt_ = random_salt;
            view_model_index_ = view_model_index;
        }

        // Checks if this session is active.
        public Boolean IsAlive()
        {
            return linear_index_ != Request.INVALID_APPS_SESSION_INDEX;
        }

        // Destroys existing session.
        public void Destroy()
        {
            linear_index_ = Request.INVALID_APPS_SESSION_INDEX;
            random_salt_ = Request.INVALID_APPS_SESSION_SALT;
        }
    }

    /// <summary>
    /// Struct ScSessionStruct
    /// </summary>
    public struct ScSessionStructOld
    {
        // Session random salt.
        /// <summary>
        /// The session random salt.
        /// </summary>
        public UInt64 gw_session_salt_;

        // Unique session linear index.
        // Points to the element in sessions linear array.
        /// <summary>
        /// The session_index_
        /// </summary>
        public UInt32 gw_session_index_;

        // Scheduler ID.
        /// <summary>
        /// The scheduler_id_
        /// </summary>
        public Byte scheduler_id_;

        /// <summary>
        /// Unique number coming from Apps.
        /// </summary>
        public UInt32 apps_unique_session_index_;

        /// <summary>
        /// Apps session salt.
        /// </summary>
        public UInt64 apps_session_salt_;

        // Session string length in characters.
        /// <summary>
        /// The S c_ SESSIO n_ STRIN g_ LE n_ CHARS
        /// </summary>
        const Int32 SC_SESSION_STRING_LEN_CHARS = 24;

        /*
        // Hex table used for conversion.
        /// <summary>
        /// The hex_table_
        /// </summary>
        static Byte[] hex_table_ = new Byte[] { (Byte)'0', (Byte)'1', (Byte)'2', (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'A', (Byte)'B', (Byte)'C', (Byte)'D', (Byte)'E', (Byte)'F' };

        // Session cookie prefix.
        /// <summary>
        /// The session cookie prefix
        /// </summary>
        const String SessionCookiePrefix = "ScSessionId: ";

        // Converts uint64_t number to hexadecimal string.
        /// <summary>
        /// Uint64_to_hex_strings the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="bytes_out">The bytes_out.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="num_4bits">The num_4bits.</param>
        /// <returns>Int32.</returns>
        Int32 uint64_to_hex_string(UInt64 number, Byte[] bytes_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while (number > 0)
            {
                bytes_out[n + offset] = hex_table_[number & 0xF];
                n++;
                number >>= 4;
            }

            // Filling with zero values if necessary.
            while (n < num_4bits)
            {
                bytes_out[n + offset] = (Byte)'0';
                n++;
            }

            // Returning length.
            return n;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="str_out">The str_out.</param>
        /// <returns>Int32.</returns>
        public Int32 ConvertToString(Byte[] str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToString()
        {
            // Allocating string bytes.
            Byte[] str_bytes = new Byte[SC_SESSION_STRING_LEN_CHARS];

            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_bytes, sessionStringLen, 16);

            // Converting byte array to string.
            return UTF8Encoding.ASCII.GetString(str_bytes);
        }

        // Converts session to a cookie string.
        /// <summary>
        /// Converts to session cookie.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToSessionCookie()
        {
            return SessionCookiePrefix + ConvertToString();
        }

        // Converts uint64_t number to hexadecimal string.
        /// <summary>
        /// Uint64_to_hex_strings the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="bytes_out">The bytes_out.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="num_4bits">The num_4bits.</param>
        /// <returns>Int32.</returns>
        unsafe Int32 uint64_to_hex_string(UInt64 number, Byte* bytes_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while (number > 0)
            {
                bytes_out[n + offset] = hex_table_[number & 0xF];
                n++;
                number >>= 4;
            }

            // Filling with zero values if necessary.
            while (n < num_4bits)
            {
                bytes_out[n + offset] = (Byte)'0';
                n++;
            }

            // Returning length.
            return n;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string unsafe.
        /// </summary>
        /// <param name="str_out">The str_out.</param>
        /// <returns>Int32.</returns>
        public unsafe Int32 ConvertToStringUnsafe(Byte* str_out)
        {
            // Translating session index.
            Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 0, 8);

            // Translating session random salt.
            sessionStringLen += uint64_to_hex_string(session_salt_, str_out, sessionStringLen, 16);

            return sessionStringLen;
        }

        // Converts session to string.
        /// <summary>
        /// Converts to string faster.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToStringFaster()
        {
            unsafe
            {
                // Allocating string bytes on stack.
                Byte* str_bytes = stackalloc Byte[SC_SESSION_STRING_LEN_CHARS];

                // Translating session index.
                Int32 sessionStringLen = uint64_to_hex_string(session_index_, str_bytes, 0, 8);

                // Translating session random salt.
                sessionStringLen += uint64_to_hex_string(session_salt_, str_bytes, sessionStringLen, 16);

                // Converting byte array to string.
                return Marshal.PtrToStringAnsi((IntPtr)str_bytes, SC_SESSION_STRING_LEN_CHARS);
            }
        }

        // Converts session to a cookie string.
        /// <summary>
        /// Converts to session cookie faster.
        /// </summary>
        /// <returns>String.</returns>
        public String ConvertToSessionCookieFaster()
        {
            return SessionCookiePrefix + ConvertToStringFaster();
        }

        /// <summary>
        /// SessionIdStub
        /// </summary>
        public const String SessionIdStub = "########################";

        /// <summary>
        /// SessionIdName
        /// </summary>
        public const String SessionIdName = "ScSsnId";

        /// <summary>
        /// SessionIdHeaderStubString_.
        /// </summary>
        public const String SessionIdHeaderPlusEndLineStubString = SessionIdName + ": " + SessionIdStub + StarcounterConstants.NetworkConstants.CRLF;

        /// <summary>
        /// SessionIdCookieStubString_
        /// </summary>
        public const String SessionIdCookiePlusEndlineStubString = "Set-Cookie: " + SessionIdName + "=" + SessionIdStub + StarcounterConstants.NetworkConstants.CRLF;

        /// <summary>
        /// SessionIdHeaderPlusEndlineStubBytes_.
        /// </summary>
        public static readonly Byte[] SessionIdHeaderPlusEndlineStubBytes = System.Text.Encoding.UTF8.GetBytes(SessionIdHeaderPlusEndLineStubString);

        /// <summary>
        /// SessionIdHeaderPlusEndlineStubBytes_.
        /// </summary>
        public static readonly Byte[] SessionIdCookiePlusEndlineStubBytes = System.Text.Encoding.UTF8.GetBytes(SessionIdCookiePlusEndlineStubString);
        */
    }
}
