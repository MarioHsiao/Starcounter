using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Framework
{
/// <summary>
/// Indicates something has gone awry during the test.
/// </summary>
[global::System.Serializable]
public class PolePositionException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public PolePositionException() { }
    public PolePositionException(string message) : base(message) { }
    public PolePositionException(string message, Exception inner) : base(message, inner) { }
    protected PolePositionException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    : base(info, context) { }
}
}
