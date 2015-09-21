
using System;
using System.Net;

namespace Starcounter.Rest.ExtensionMethods {
    /// <summary>
    /// Provides a set of extension methods for the <see cref="Node"/>
    /// class.
    /// </summary>
    public static class NodeExtensions {
        
        public static string ToLocal(this Node node, string absoluteOrRelative) {
            return ToLocal(node, new Uri(absoluteOrRelative)).ToString();
        }

        public static Uri ToLocal(this Node node, Uri absoluteOrRelative) {
            if (!absoluteOrRelative.IsAbsoluteUri)
                return absoluteOrRelative;

            if (IsLocal(node.BaseAddress)) {
                // The node represents a local node. We force the absolute one
                // to be so too.
                if (IsLocal(absoluteOrRelative)) {
                    Uri result;
                    Uri.TryCreate(absoluteOrRelative.PathAndQuery, UriKind.Relative, out result);
                    return result;
                }
                throw new ArgumentOutOfRangeException("absolute");
            }

            // If the node is not local, we check to see that the absolute
            // address refers to the same host.

            if (IsLocal(absoluteOrRelative)) {
                throw new ArgumentOutOfRangeException("absolute");
            }

            if (absoluteOrRelative.Host != node.HostName)
                throw new ArgumentOutOfRangeException("absolute");

            return new Uri(absoluteOrRelative.PathAndQuery);
        }

        public static bool IsLocal(Uri uri) {
            if (uri.IsLoopback)
                return true;

            var host = Dns.GetHostEntry(String.Empty).HostName;
            return uri.Host.Equals(host, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
