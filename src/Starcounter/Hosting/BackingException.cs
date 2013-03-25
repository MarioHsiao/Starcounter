
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents a group of errors that can occur when backing constructs
    /// are being consumed by a host, as laid out 
    /// <a href="http://www.starcounter.com/internal/wiki/W3"> here</a>.
    /// </summary>
    public class BackingException : DbException {

        internal BackingException WithPostfix(uint errorCode, string postfix, params object[] arguments) {
            var message = Starcounter.ErrorCode.ToMessage(errorCode, string.Format(postfix, arguments));
            return new BackingException(errorCode, message);
        }

        internal BackingException(uint errorCode)
            : this(errorCode, Starcounter.ErrorCode.ToMessage(errorCode)) {
        }

        internal BackingException(uint errorCode, string message)
            : base(errorCode, message) {
        }

        internal BackingException(uint errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException) {
        }
    }
}