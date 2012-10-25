using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.ABCIPC;

namespace Starcounter.VisualStudio {
    /// <summary>
    /// Exposes a set of utility methods for interaction with the server.
    /// </summary>
    internal static class ServerUtility {
        /// <summary>
        /// Deserializes the carry of a reply coming from the ABCIPC-based
        /// server services, built-in in the core server engine.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reply"></param>
        /// <returns></returns>
        internal static T DeserializeCarry<T>(Reply reply) {
            string replyData;
            T result;

            result = reply.TryGetCarry(out replyData)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<T>(replyData)
                : default(T);

            return result;
        }
    }
}