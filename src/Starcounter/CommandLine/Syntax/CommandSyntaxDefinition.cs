// ***********************************************************************
// <copyright file="CommandSyntaxDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Class CommandSyntaxDefinition
    /// </summary>
    public sealed class CommandSyntaxDefinition : SyntaxDefinition, ICommandSyntax
    {
        /// <summary>
        /// The name
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// The description
        /// </summary>
        public readonly string Description;
        /// <summary>
        /// Gets or sets the min parameter count.
        /// </summary>
        /// <value>The min parameter count.</value>
        public int? MinParameterCount { get; set; }
        /// <summary>
        /// Gets or sets the max parameter count.
        /// </summary>
        /// <value>The max parameter count.</value>
        public int? MaxParameterCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSyntaxDefinition" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        internal CommandSyntaxDefinition(string name, string description)
            : base()
        {
            this.Name = name;
            this.Description = description;
            this.MinParameterCount = null;
            this.MaxParameterCount = null;
        }

        #region ICommandSyntax Members

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string ICommandSyntax.Name
        {
            get { return this.Name; }
        }

        /// <summary>
        /// Gets the command description.
        /// </summary>
        /// <value>The command description.</value>
        string ICommandSyntax.CommandDescription
        {
            get { return this.Description; }
        }

        /// <summary>
        /// Gets the min parameter count.
        /// </summary>
        /// <value>The min parameter count.</value>
        int? ICommandSyntax.MinParameterCount
        {
            get { return this.MinParameterCount; }
        }

        /// <summary>
        /// Gets the max parameter count.
        /// </summary>
        /// <value>The max parameter count.</value>
        int? ICommandSyntax.MaxParameterCount
        {
            get { return this.MaxParameterCount; }
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        OptionInfo[] ICommandSyntax.Properties
        {
            get
            {
                return this.CreateOptionSet(this.Properties);
            }
        }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        /// <value>The flags.</value>
        OptionInfo[] ICommandSyntax.Flags
        {
            get
            {
                return this.CreateOptionSet(this.Flags);
            }
        }

        #endregion
    }
}
