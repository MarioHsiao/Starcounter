

using Starcounter.Advanced;

namespace Starcounter {

    /// <summary>
    /// A message that is automatically populated by a bound data object (typically a
    /// database Entity).
    /// A message is a temporary object typically serialized to a Json text as part
    /// of a response to a request or deserialized from a Json text as a part of data
    /// in a request.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Message<T> : Obj<T> where T : IBindable {
    }

    /// <summary>
    /// A message is a temporary object typically serialized to a Json text as part
    /// of a response to a request or deserialized from a Json text as a part of data
    /// in a request.
    /// </summary>
    public class Message : Message<NullData> {
    }
}
