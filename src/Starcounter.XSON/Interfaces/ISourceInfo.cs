namespace Starcounter.XSON.Interfaces {
    /// <summary>
    /// 
    /// </summary>
    public interface ISourceInfo {
        /// <summary>
        /// Gets the name of the sourcefile.
        /// </summary>
        /// <value>The name of the file.</value>
        string Filename { get; }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line no.</value>
        int Line { get; }

        /// <summary>
        /// Gets the column number.
        /// </summary>
        /// <value>The col no.</value>
        int Column { get;  }        
    }
}


