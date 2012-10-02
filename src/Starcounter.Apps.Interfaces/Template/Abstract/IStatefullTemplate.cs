
using System;
namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// Template for elements that can be edited or have their state changed or that can contain child
    /// elements (properties or array elements) that can be edited or have their state changed. This is 
    /// true for all elements exept for Action elements (i.e. the ActionTemplate does not inherit this class).
    /// </summary>
    public interface IStatefullTemplate : ITemplate {

        bool Editable { get; set; }
        
        /// <summary>
        /// As this template is represented by a runtime statefull object or value, we need to know how to create
        /// a that object or value.
        /// </summary>
        /// <param name="parent">The host of the new object. Either a App or a AppList</param>
        /// <returns>The value or object. For instance, if this is a StringTemplate, the default string
        /// for the property to be in the new App object is returned.</returns>
        object CreateInstance( IAppNode parent );

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        Type InstanceType { get; }

    }
}
