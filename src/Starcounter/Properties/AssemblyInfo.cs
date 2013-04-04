using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Starcounter")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Starcounter AB")]
[assembly: AssemblyProduct("Starcounter")]
[assembly: AssemblyCopyright("Copyright © Starcounter AB 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4548b98e-eed7-4115-b8ad-b280db692f95")]
[assembly: InternalsVisibleTo("Starcounter.Apps")]

// TODO:
// This should be removed when DisplayName on sqlresults 
// (TypeBinding, PropertyBinding, PropertyMapping) is publicly exposed.
[assembly: InternalsVisibleTo("Starcounter.Apps.JsonPatch")]

// Allow Starcounter.Hosting to access the internals of the VMDBMS.
[assembly: InternalsVisibleTo("Starcounter.Hosting")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]
