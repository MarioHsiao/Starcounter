// ***********************************************************************
// <copyright file="DatabaseSchemaElement.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sc.Server.Weaver.Schema
{
    /// <summary>
    /// Class DatabaseSchemaElement
    /// </summary>
[Serializable]
public abstract class DatabaseSchemaElement
{
    /// <summary>
    /// The tags
    /// </summary>
    [NonSerialized]
    private Dictionary<string, object> tags = new Dictionary<string, object>();

    /// <summary>
    /// The serialized tags
    /// </summary>
    private Dictionary<string, object> serializedTags = new Dictionary<string, object>();

    /// <summary>
    /// Called when [deserialized initialize tags].
    /// </summary>
    /// <param name="context">The context.</param>
    [OnDeserializedAttribute]
    private void OnDeserializedInitializeTags(
        StreamingContext context
    )
    {
        this.tags = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <value>The tags.</value>
    public IDictionary<string, object> Tags
    {
        get
        {
            return tags;
        }
    }

    /// <summary>
    /// Gets the serialized tag.
    /// </summary>
    /// <value>The serialized tag.</value>
    public IDictionary<string, object> SerializedTag
    {
        get
        {
            return serializedTags;
        }
    }

    /// <summary>
    /// Gets the <see cref="DatabaseSchema" /> to which the current object belong.
    /// </summary>
    /// <value>The schema.</value>
    public abstract DatabaseSchema Schema
    {
        get;
    }
}
}