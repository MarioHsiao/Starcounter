
namespace Starcounter.Internal.JsonTemplate


open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices



module Version =
  let [<Literal>] Major = 0
  let [<Literal>] Minor = 2
  let [<Literal>] Build = 1
  let [<Literal>] Revision = 0
  let [<Literal>] String = "0.2.1.0"
  let Tupled = Major, Minor, Build, Revision
  let FullName = sprintf "Starcounter Internal Template Parser %s" String
 
[<assembly: AssemblyTitle("Starcounter Internal Template Parser")>]
[<assembly: AssemblyDescription("Used to parse the Ecmascript language ")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("IronJS")>]
[<assembly: AssemblyProduct("IronJS")>]
[<assembly: AssemblyCopyright("Copyright © Starcounter AB, 2012")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]
 
[<assembly: ComVisible(false)>]

[<assembly: AssemblyVersion(Version.String)>]
[<assembly: AssemblyFileVersion(Version.String)>]

()