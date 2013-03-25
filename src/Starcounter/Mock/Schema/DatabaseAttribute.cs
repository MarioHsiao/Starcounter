// ***********************************************************************
// <copyright file="DatabaseAttribute.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents a database attribute.
/// </summary>
/// <remarks>
/// See the property <see cref="DatabaseAttribute.AttributeType"/> to determine if the
/// attribute is a persistent field, a regular field or a persistent property.
/// </remarks>
[Serializable]
public class DatabaseAttribute : DatabaseSchemaElement
{
    private DatabaseAttributeKind attributeKind;
    private readonly string name;
    private ushort index;
    private bool isInitOnly;
    private IDatabaseAttributeType attributeType;
    private bool isNullable;
    private DatabaseAttributeRef synonymTo;
//    private DatabaseAttributeRef enumerableRelatesTo;
    private int weaverId = -1;
    private readonly DatabaseClass declaringClass;
    private object initialValue;
    private DatabaseAttributeRef backingField;

    // private string indexField;
    private DatabasePersistentProperty persistentProperty;

    /// <summary>
    /// Initializes a new <see cref="DatabaseAttribute"/>.
    /// </summary>
    /// <param name="declaringClass">Class declaring the attribute.</param>
    /// <param name="name">Name of the attribute.</param>
    public DatabaseAttribute(DatabaseClass declaringClass, string name)
    {
        this.declaringClass = declaringClass;
        this.name = name;
    }

    /// <summary>
    /// Gets the class declaring the attribute.
    /// </summary>
    public DatabaseClass DeclaringClass
    {
        get
        {
            return declaringClass;
        }
    }

    /// <summary>
    /// Gets the CLR name of the field.
    /// </summary>
    public string Name
    {
        get
        {
            return name;
        }
    }

    /// <summary>
    /// Gets or sets the field index in the database kernel.
    /// </summary>
    /// <remarks>
    /// <para>This field is set only <i>after</i> calls the method see cref="ITypeResolver.SetSchema".</para>
    /// <para>This property is only meaningful for persistent fields that are not synonymous.</para>
    /// </remarks>
    public ushort Index
    {
        get
        {
            return index;
        }
        set
        {
            index = value;
        }
    }

    /// <summary>
    /// Determines whether the field is forbidden to be set outside a constructor (<b>readonly</b> qualifier
    /// in C#).
    /// </summary>
    public bool IsInitOnly
    {
        get
        {
            return isInitOnly;
        }
        set
        {
            isInitOnly = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is public read.
    /// </summary>
    /// <value><c>true</c> if this instance is public read; otherwise, <c>false</c>.</value>
    public bool IsPublicRead { get; set; }

//    public bool IsPublicWrite { get; set; }

    /// <summary>
    /// Gets the type of the attribute.
    /// </summary>
    public IDatabaseAttributeType AttributeType
    {
        get
        {
            return DatabaseClassRef.Resolve(this.attributeType, this);
        }
        set
        {
            this.attributeType = DatabaseClassRef.MakeRef(value);
        }
    }

    /// <summary>
    /// Determines whether the database attribute is nullable.
    /// </summary>
    /// <remarks>
    /// Value types normally not nullable, unless their type is <see cref="Nullable{T}"/>
    /// (<b>?</b> type postfix in C#). Reference types are always nullable. In the future,
    /// we may define a custom attribute forbidden null values to be assigned on reference
    /// types.
    /// </remarks>
    public bool IsNullable
    {
        get
        {
            return isNullable;
        }
        set
        {
            isNullable = value;
        }
    }

    /// <summary>
    /// Gets or sets the database attribute of which the current attribute is a synonymous.
    /// </summary>
    /// <value>
    /// The <see cref="DatabaseAttribute"/> of which the current attribute is a synonymous,
    /// or <b>null</b> if the current attribute is not a synonymous of any field.
    /// </value>
    public DatabaseAttribute SynonymousTo
    {
        get
        {
            return DatabaseAttributeRef.Resolve(this.synonymTo, this);
        }
        set
        {
            synonymTo = DatabaseAttributeRef.MakeRef(value);
        }
    }

    // <summary>
    // Gets or sets the 'parent' database field to which the current enumerable field relates,
    // when the current field is an enumerable (one-to-many relationship).
    // </summary>
    // <value>
    // The database attribute that is the second end of the one-to-many relationship, of <b>null</b>
    // if the current attribute is not an enumerable.
    // </value>
	//public DatabaseAttribute EnumerableRelatesTo
	//{
	//    get
	//    {
	//        return DatabaseAttributeRef.Resolve(enumerableRelatesTo, this);
	//    }
	//    set
	//    {
	//        enumerableRelatesTo = DatabaseAttributeRef.MakeRef(value);
	//    }
	//}

    /// <summary>
    /// Unique identifier of the current custom attribute, as assigned by the weaver.
    /// </summary>
    /// <remarks>
    /// This identifier is during the resolution of the database schema. Passing the field
    /// identifier instead of the complete field reduces the volume of serialized data and
    /// improves performance.
    /// </remarks>
    public int WeaverId
    {
        get
        {
            return weaverId;
        }
        set
        {
            weaverId = value;
        }
    }

    /// <summary>
    /// Gets or sets the initial value of the current attribute.
    /// </summary>
    /// <value>
    /// The initial value of the proper type (typically a boxed intrincic), or <b>null</b>
    /// if the initial value has not been set (which means that the initial value should
    /// be the default value for this type, i.e. the zero value).
    /// </value>
    public object InitialValue
    {
        get
        {
            return initialValue;
        }
        set
        {
            initialValue = value;
        }
    }


    /// <summary>
    /// Gets the kind (persistent field, regular field, persistent property) of attribute
    /// the current instance is.
    /// </summary>
    public DatabaseAttributeKind AttributeKind
    {
        get
        {
            return attributeKind;
        }
        set
        {
            attributeKind = value;
        }
    }

    // <summary>
    // When the current attribute is a persistent property, gets or sets the name
    // of the field containing the index of the current persistent property.
    // </summary>
    //public string IndexField
    //{
    //    get { return indexField; }
    //    set { indexField = value; }
    //}

    /// <summary>
    /// When the current attribute is a persistent property, gets or sets attached
    /// information about that property.
    /// </summary>
    public DatabasePersistentProperty PersistentProperty
    {
        get
        {
            return persistentProperty;
        }
        set
        {
            persistentProperty = value;
        }
    }

    /// <summary>
    /// Determines whether the current attribute is persistent.
    /// </summary>
    public bool IsPersistent
    {
        get
        {
            return this.attributeKind == DatabaseAttributeKind.PersistentField ||
                   this.attributeKind == DatabaseAttributeKind.PersistentProperty;
        }
    }

    /// <summary>
    /// Determines whether the current attribute is a field (persistent or not).
    /// </summary>
    public bool IsField
    {
        get
        {
            return this.attributeKind == DatabaseAttributeKind.PersistentField ||
                   this.attributeKind == DatabaseAttributeKind.NonPersistentField;
        }
    }

    /// <summary>
    /// Indicates if this is as a persistent property mapping to a
    /// kernel database field that property doesnt define itself, but one
    /// that is rather defined previously.
    /// </summary>
    public Boolean IsPropertyMapToExternalField
    {
        get
        {
            return this.persistentProperty != null &&
                   !String.IsNullOrEmpty(this.persistentProperty.KernelFieldMapping);
        }
    }

    /// <summary>
    /// Gets the <see cref="DatabaseSchema"/> to which the current database attribute belongs.
    /// </summary>
    public override DatabaseSchema Schema
    {
        get
        {
            return this.declaringClass.Schema;
        }
    }

    /// <summary>
    /// When not null, indicates that the proprerty is the trivial accessor for a field of the current instance.
    /// This property is then set to this this field.
    /// </summary>
    public DatabaseAttribute BackingField
    {
        get
        {
            return DatabaseAttributeRef.Resolve(this.backingField, this);
        }
        set
        {
            backingField = DatabaseAttributeRef.MakeRef(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat(" {0} : {1}, {2}", this.name, this.attributeKind, this.attributeType);
        if (this.isInitOnly)
        {
            builder.Append(", init only");
        }
        if (this.isNullable)
        {
            builder.Append(", nullable");
        }
        if (this.synonymTo != null)
        {
            builder.AppendFormat(", synonym to {0}", this.synonymTo);
        }
		//if (this.enumerableRelatesTo != null)
		//{
		//    builder.AppendFormat(", relates to {0}", this.enumerableRelatesTo);
		//}
        if (this.initialValue != null)
        {
            builder.AppendFormat(", initial value = {0}", this.initialValue);
        }
        if (this.persistentProperty != null)
        {
            if (this.persistentProperty.AttributeFieldIndex != null)
            {
                builder.AppendFormat(", index field = {0}", this.persistentProperty.AttributeFieldIndex);
            }
            if (this.persistentProperty.KernelFieldMapping != null)
            {
                builder.AppendFormat(", kernel field = {0}", this.persistentProperty.KernelFieldMapping);
            }
        }
        return builder.ToString();
    }
}

/// <summary>
/// 
/// </summary>
[Serializable]
public class DatabaseAttributeCollection : KeyedCollection<string, DatabaseAttribute>
{

    /// <summary>
    /// 
    /// </summary>
    public DatabaseAttributeCollection()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected override string GetKeyForItem(DatabaseAttribute item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }
        string name = item.Name;
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("item.Name has not been set.");
        }
        return name;
    }

    //PI110503
    //// Added to support case insensitivity.
    //internal Boolean Contains_CaseInsensitive(String inName, out String outName)
    //{
    //    String inNameUpperCase = inName.ToUpper();
    //    IEnumerator<DatabaseAttribute> dbAttrEnum = this.GetEnumerator();
    //    while (dbAttrEnum.MoveNext())
    //    {
    //        if (dbAttrEnum.Current.Name.ToUpper() == inNameUpperCase)
    //        {
    //            outName = dbAttrEnum.Current.Name;
    //            return true;
    //        }
    //    }
    //    outName = null;
    //    return false;
    //}
}

/// <summary>
/// 
/// </summary>
public class DatabaseAttributeArrayOnIndexCompararer : IComparer
{
    #region IComparer Members

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int Compare(object x, object y)
    {
        // x < y -1, = 0, x > y +1
        DatabaseAttribute ax, ay;
        ax = x as DatabaseAttribute;
        ay = y as DatabaseAttribute;
        return ax.Index < ay.Index ? -1 : ax.Index == ay.Index ? 0 : +1;
    }

    #endregion
}

/// <summary>
/// 
/// </summary>
public enum DatabaseAttributeKind
{
    /// <summary>
    /// Regular persistent field.
    /// </summary>
    PersistentField,

    /// <summary>
    /// Non-persistent (and non queriable) field.
    /// </summary>
    NonPersistentField,

    /// <summary>
    /// Property that maps to a persistent field (internal use only).
    /// </summary>
    PersistentProperty,

    /// <summary>
    /// Non-persistent (regular) property.
    /// </summary>
    NotPersistentProperty
}

/// <summary>
/// 
/// </summary>
[Serializable]
public sealed class DatabasePersistentProperty
{
    /// <summary>
    /// 
    /// </summary>
    public String AttributeFieldIndex;
    /// <summary>
    /// 
    /// </summary>
    public String KernelFieldMapping;
}
}