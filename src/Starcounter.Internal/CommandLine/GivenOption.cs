// ***********************************************************************
// <copyright file="GivenOption.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.CommandLine.Syntax;
using System;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Represents an option specified on the command line.
    /// </summary>
    internal class GivenOption
    {
        /// <summary>
        /// Gets or sets the information about the option, in the
        /// form of an <see cref="OptionInfo"/> instance.
        /// </summary>
        internal OptionInfo Option { get; set; }

        /// <summary>
        /// The name by which the option was given.
        /// </summary>
        internal string SpecifiedName;

        /// <summary>
        /// The options value.
        /// </summary>
        internal string Value;

        /// <summary>
        /// The section in where the option was given.
        /// </summary>
        internal CommandLineSection Section;

        /// <summary>
        /// Gets a value indicating if the given option represents
        /// a flag.
        /// </summary>
        /// <value><c>true</c> if this instance is a flag; <c>false</c> 
        /// otherwise.</value>
        internal bool IsFlag {
            get {
                return (Option.Attributes & OptionAttributes.Flag) != 0;
            }
        }
        
        /// <summary>
        /// Converts the value of the current <see cref="GivenOption"/> 
        /// to a string representation using the specified format.
        /// </summary>
        /// <param name="format">The format to use when formatting.</param>
        /// <returns>A string representation of the current <see cref="GivenOption"/>
        /// using the specified format.</returns>
        public string ToString(string format) {
            format = format ?? string.Empty;
            string result;

            format = format.ToLowerInvariant();
            switch (format) {
                case "":
                    result = base.ToString();
                    break;
                case "standard":
                    result = string.Concat(Parser.StandardOptionPrefix, Option.Name);
                    if (!IsFlag) {
                        result += string.Concat(Parser.StandardOptionSuffix, Value);
                    }
                    break;
                case "given":
                    result = string.Concat(Parser.StandardOptionPrefix, SpecifiedName);
                    if (!IsFlag) {
                        result += string.Concat(Parser.StandardOptionSuffix, Value);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("format");
            }

            return result;
        }
    }
}