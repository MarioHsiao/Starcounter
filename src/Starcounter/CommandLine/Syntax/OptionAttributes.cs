
using System;

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Defines a set of constant flags that can be use to attribute
    /// an option.
    /// </summary>
    [Flags] public enum OptionAttributes
    {
        /// <summary>
        /// Specifies the default option attributes are to be used.
        /// </summary>
        /// <remarks>
        /// The default is a property that is not required and can
        /// only appear once.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// Specifies the option to which the current attribute
        /// applies is required.
        /// </summary>
        Required = 1,

        /// <summary>
        /// Specifies the option to which the current attribute
        /// applies is a flag, and hence no value can be expected.
        /// </summary>
        Flag = 2
    }
}
