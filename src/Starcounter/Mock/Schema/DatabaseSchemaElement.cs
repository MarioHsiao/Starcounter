using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sc.Server.Weaver.Schema
{
[Serializable]
public abstract class DatabaseSchemaElement
{
    [NonSerialized]
    private Dictionary<string, object> tags = new Dictionary<string, object>();

    private Dictionary<string, object> serializedTags = new Dictionary<string, object>();

    [OnDeserializedAttribute]
    private void OnDeserializedInitializeTags(
        StreamingContext context
    )
    {
        this.tags = new Dictionary<string, object>();
    }

    public IDictionary<string, object> Tags
    {
        get
        {
            return tags;
        }
    }

    public IDictionary<string, object> SerializedTag
    {
        get
        {
            return serializedTags;
        }
    }

    /// <summary>
    /// Gets the <see cref="DatabaseSchema"/> to which the current object belong.
    /// </summary>
    public abstract DatabaseSchema Schema
    {
        get;
    }
}
}