using System;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents an array type in the database schema.
/// </summary>
[Serializable]
public class DatabaseArrayType : DatabaseSchemaElement, IDatabaseAttributeType
{
    private readonly IDatabaseAttributeType itemType;
    private readonly DatabaseSchemaElement parent;

    /// <summary>
    /// Initializes a new <see cref="DatabaseArrayType"/>.
    /// </summary>
    /// <param name="itemType">Type of array items.</param>
    public DatabaseArrayType(DatabaseSchemaElement parent, IDatabaseAttributeType itemType)
    {
        this.itemType = DatabaseClassRef.MakeRef(itemType);
        this.parent = parent;
    }

    /// <summary>
    /// Gets the type of array elements.
    /// </summary>
    public IDatabaseAttributeType ItemType
    {
        get
        {
            return DatabaseClassRef.Resolve(itemType, this);
        }
    }

    public override string ToString()
    {
        return "array of " + this.itemType.ToString();
    }


    #region IDatabaseSchemaElement Members

    public override DatabaseSchema Schema
    {
        get
        {
            return this.parent.Schema;
        }
    }

    #endregion
}
}