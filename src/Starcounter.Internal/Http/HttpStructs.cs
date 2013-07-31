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
        /// HTTP GET.
        /// </summary>
        GET,

        /// <summary>
        /// HTTP POST.
        /// </summary>
        POST,

        /// <summary>
        /// HTTP PUT.
        /// </summary>
        PUT,

        /// <summary>
        /// HTTP PATCH.
        /// </summary>
        PATCH,

        /// <summary>
        /// HTTP DELETE.
        /// </summary>
        DELETE,

        /// <summary>
        /// HTTP HEAD.
        /// </summary>
        HEAD,

        /// <summary>
        /// HTTP OPTIONS.
        /// </summary>
        OPTIONS,

        /// <summary>
        /// HTTP TRACE.
        /// </summary>
        TRACE,

        /// <summary>
        /// Other HTTP method.
        /// </summary>
        OTHER_METHOD
    };

    public class Helper
    {
        /// <summary>
        /// Gets method enumeration from given string.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static HTTP_METHODS GetMethodFromString(String method)
        {
            foreach (HTTP_METHODS m in Enum.GetValues(typeof(HTTP_METHODS)))
            {
                if (method.StartsWith(m.ToString()))
                    return m;
            }

            return HTTP_METHODS.OTHER_METHOD;
        }
    }

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
        public UInt32 reserved_;

        public void Init(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt,
            UInt32 reserved)
        {
            scheduler_id_ = scheduler_id;
            linear_index_ = linear_index;
            random_salt_ = random_salt;
            reserved_ = reserved;
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
}
