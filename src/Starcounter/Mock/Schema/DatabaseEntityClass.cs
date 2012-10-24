// ***********************************************************************
// <copyright file="DatabaseEntityClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents an entity class, i.e. a class derived from the <b>Entity</b>
/// class.
/// </summary>
[Serializable]
public class DatabaseEntityClass : DatabaseClass
{
    [NonSerialized]
    private  List<DatabaseExtensionClass> extensionClasses
    = new List<DatabaseExtensionClass>();


    /// <summary>
    /// Initializes a new <see cref="DatabaseEntityClass"/>.
    /// </summary>
    /// <param name="assembly">Assembly to which the class belong.</param>
    /// <param name="name">Class name.</param>
    public DatabaseEntityClass(DatabaseAssembly assembly, string name)
		: base(assembly, name)
    {
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="assembly"></param>
	/// <param name="name"></param>
	/// <param name="internalMetadataClass"></param>
	public DatabaseEntityClass(DatabaseAssembly assembly, string name, Boolean internalMetadataClass)
		: base(assembly, name, internalMetadataClass)
	{
	}

    [OnDeserialized]
    private void OnDeserializedInitExtensionClasses(StreamingContext context)
    {
        this.extensionClasses = new List<DatabaseExtensionClass>();
    }


    /// <summary>
    /// Gets the collection of extension classes defined for the current entity class.
    /// </summary>
    public IList<DatabaseExtensionClass> ExtensionClasses
    {
        get
        {
            return this.extensionClasses;
        }
    }

    /// <summary>
    /// Formats the current assembly and all its members to a writer.
    /// </summary>
    /// <param name="writer">The writer to which the object should be formatted.</param>
    public override void DebugOutput(IndentedTextWriter writer)
    {
        base.DebugOutput(writer);
        if (this.extensionClasses.Count > 0)
        {
            writer.Indent++;
            writer.WriteLine("Extension Classes:");
            writer.Indent++;
            foreach (DatabaseExtensionClass extensionClass in this.extensionClasses)
            {
                extensionClass.DebugOutput(writer);
                writer.WriteLine();
            }
            writer.Indent --;
            writer.Indent--;
        }
    }
}
}