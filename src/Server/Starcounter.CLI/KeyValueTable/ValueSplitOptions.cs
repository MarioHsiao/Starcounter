using System;

namespace Starcounter.CLI {
    /// <summary>
    /// Specifies a set of predefined ways to split up given
    /// values when writing to a <see cref="KeyValueTable"/>.
    /// </summary>
    public enum ValueSplitOptions {
        /// <summary>
        /// Dont split
        /// </summary>
        None,
        /// <summary>
        /// Split the value up based on the <see cref="Environment.NewLine"/>
        /// seqence.
        /// </summary>
        SplitLines
    }
}