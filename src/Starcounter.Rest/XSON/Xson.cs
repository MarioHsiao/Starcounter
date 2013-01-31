
namespace Starcounter {

    /// <summary>
    /// Xson is a Json inspired implementation of Object trees supporting arrays and basic
    /// value types.
    /// 
    /// Xson is the base class for Starcounter Applets and Starcounter Messages.
    /// 
    /// Each object points to a Template that describes its schema (properties). 
    /// 
    /// The datatypes are a merge of what is available in most common high abstraction application languages such as Javascript,
    /// C#, Ruby and Java. This means that it is in part a superset and in part a subset.
    /// 
    /// The difference from the close relative the Json induced object tree in Javascript is
    /// foremost that Xson supports multiple numeric types, time and higher precision numerics.
    ///
    /// The types supported are:
    ///
    /// Object			    (can contain properties of any supported type)
    /// List			    (typed array/list/vector of any supported type),
    /// null            
    /// Time 			    (datetime)
    /// Boolean
    /// String 			    (variable length Unicode string),
    /// Integer 		    (variable length up to 64 bit, signed)
    /// Unsigned Integer	(variable length up to 64 bit, unsigned)
    /// Decimal			    (base-10 floating point up to 64 bit),
    /// Float			    (base-2 floating point up to 64 bit)
    /// 
    /// 
    /// The object trees are designed to be serializable and deserializable to and from JSON and XML although there
    /// is presently no XML implementation.
    /// 
    /// When you write applications in Starcounter, you normally do not use Xson objects directly. Instead you would
    /// use the specialisations Applet for session-bound object trees or Message for REST style data transfer objects
    /// that are sent as requests or responses to and from a Starcounter REST endpoint (handler).
    /// </summary>
    /// <remarks>
    /// Although made up only after the acronym Xson was chosen, the acronym stands for "eXchangeable Serialized Object Nodes".
    /// 
    /// The current implementation has a few shortcommings. Currently Xson only supports arrays of objects.
    /// Also, all objects in the array must use the same template. Support for arrays of value types (primitives) will
    /// be supported in the future. Mixed type arrays are currently not planned.
    /// 
    /// In the release version of Starcounter, Xson objects trees will be optimized for storage in "blobs" rather than on
    /// the garbage collected heap. This is such that stateful sessions can employ them without causing unnecessary system
    /// stress.
    /// </remarks>
    public class Xson {
    }
}
