﻿// ***********************************************************************
// <copyright file="OptionInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.CommandLine.Syntax {
    /// <summary>
    /// Class OptionInfo
    /// </summary>
    public class OptionInfo {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the alternative names.
        /// </summary>
        /// <value>The alternative names.</value>
        public string[] AlternativeNames { get; set; }

        /// <summary>
        /// Gets an array including all the names of the current
        /// option, starting with the default name.
        /// </summary>
        public string[] AllNames {
            get {
                var alternatives = this.AlternativeNames;

                if (alternatives == null || alternatives.Length == 0)
                    return new string[] { this.Name };

                string[] all = new string[alternatives.Length + 1];
                Array.Copy(alternatives, 0, all, 1, alternatives.Length);
                all[0] = this.Name;

                return all;
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>The attributes.</value>
        public OptionAttributes Attributes { get; set; }

        /// <summary>
        /// Determines whether the specified name has name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name has name; otherwise, <c>false</c>.</returns>
        public bool HasName(string name) {
            return HasName(name, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified name has name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="comparisonMethod">The comparison method.</param>
        /// <returns><c>true</c> if the specified name has name; otherwise, <c>false</c>.</returns>
        public bool HasName(string name, StringComparison comparisonMethod) {
            if (this.Name.Equals(name, comparisonMethod))
                return true;

            if (this.AlternativeNames == null)
                return false;

            foreach (string alternative in this.AlternativeNames) {
                if (alternative.Equals(name, comparisonMethod))
                    return true;
            }

            return false;
        }
    }
}