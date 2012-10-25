// ***********************************************************************
// <copyright file="DatabaseIndex.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;

namespace Sc.Server.Weaver.Schema
{

    /// <summary>
    /// Class DatabaseIndex
    /// </summary>
	[Serializable]
	internal class DatabaseIndex : Object
	{
		internal readonly String Name;
		internal readonly String DatabaseClassName;
		internal readonly String[] AttributeNames;
		internal readonly int[] SortOrders;
		internal readonly Boolean Unique;

		private DatabaseClass _databaseClass;
		private DatabaseAttribute[] _attributes;

		internal DatabaseIndex(String indexName,
							   DatabaseClass databaseClass,
							   DatabaseAttribute[] attributes,
							   int[] sortOrders,
							   Boolean unique)
		{
			Name = indexName;
			_databaseClass = databaseClass;
			DatabaseClassName = databaseClass.Name;
			_attributes = attributes;
			SortOrders = sortOrders;
			Unique = unique;
        }

		internal DatabaseIndex(String indexName,
							   String databaseClassName,
							   String[] attributeNames,
							   int[] sortOrders,
							   Boolean unique)
		{
			Name = indexName;
			DatabaseClassName = databaseClassName;
			AttributeNames = attributeNames;
			SortOrders = sortOrders;
			Unique = unique;
			_databaseClass = null;
			_attributes = null;
        }

		internal DatabaseClass DataBaseClass
		{
			get { return _databaseClass; }
		}

		internal DatabaseAttribute[] Attributes
		{
			get { return _attributes; }
		}

        internal void SetSchemaInformation(DatabaseSchema schema)
        {
            DatabaseAttribute[] dbaArr;

            _databaseClass = GetDatabaseClass(schema);

            dbaArr = new DatabaseAttribute[AttributeNames.Length];
            for (Int32 i = 0; i < dbaArr.Length; i++)
            {
                //dbaArr[i] = _databaseClass.Attributes[AttributeNames[i]];
                dbaArr[i] = _databaseClass.FindAttributeInAncestors(AttributeNames[i]);
                if (dbaArr[i] == null)
                {
                    throw new Exception(String.Format("Field {0} does not exist in class {1} (index: {2}).",
                                                         AttributeNames[i],
                                                         DatabaseClassName,
                                                         Name));
                }
            }
            _attributes = dbaArr;
        }

        //PI110503
        //// Added to support case insensitivity.
        //internal void SetSchemaInformation_CaseInsensitive(DatabaseSchema schema)
        //{
        //    DatabaseAttribute[] dbaArr;

        //    _databaseClass = GetDatabaseClass_CaseInsensitive(schema);

        //    dbaArr = new DatabaseAttribute[AttributeNames.Length];
        //    for (Int32 i = 0; i < dbaArr.Length; i++)
        //    {
        //        //dbaArr[i] = _databaseClass.Attributes[AttributeNames[i]];
        //        dbaArr[i] = _databaseClass.FindAttributeInAncestors_CaseInsensitive(AttributeNames[i]);
        //        if (dbaArr[i] == null)
        //        {
        //            throw new Exception(String.Format("Field {0} does not exist in class {1} (index: {2}).",
        //                                                 AttributeNames[i],
        //                                                 DatabaseClassName,
        //                                                 Name));
        //        }
        //    }
        //    _attributes = dbaArr;
        //}

        private DatabaseClass GetDatabaseClass(DatabaseSchema schema)
        {
            Boolean found;
            DatabaseClass dbClass;

            if (DatabaseClassName.IndexOf('.') == -1) // Shortname
            {
                found = schema.FindDatabaseClassByShortname(DatabaseClassName,
                                                            out dbClass);
                if (dbClass == null)
                {
                    if (found) // Ambiguous match
                    {
                        throw new Exception(String.Format("Ambiguous entity type '{0}' in index {1}.",
                                                             DatabaseClassName,
                                                             Name));
                    }
                    else
                    {
                        throw new Exception(String.Format("Unknown entity type '{0}' in index {1}.",
                                                             DatabaseClassName,
                                                             Name));
                    }
                }
            }
            else
            {
                dbClass = schema.FindDatabaseClass(DatabaseClassName);
                if (dbClass == null)
                {
                    throw new Exception(String.Format("Unknown entity type '{0}' in index {1}.",
                                                         DatabaseClassName,
                                                         Name));
                }
            }
            return dbClass;
        }

        //PI110503
        //// Added to support case insensitivity.
        //private DatabaseClass GetDatabaseClass_CaseInsensitive(DatabaseSchema schema)
        //{
        //    Boolean found;
        //    DatabaseClass dbClass;

        //    if (DatabaseClassName.IndexOf('.') == -1) // Shortname
        //    {
        //        found = schema.FindDatabaseClassByShortname_CaseInsensitive(DatabaseClassName,
        //                                                    out dbClass);
        //        if (dbClass == null)
        //        {
        //            if (found) // Ambiguous match
        //            {
        //                throw new Exception(String.Format("Ambiguous entity type '{0}' in index {1}.",
        //                                                     DatabaseClassName,
        //                                                     Name));
        //            }
        //            else
        //            {
        //                throw new Exception(String.Format("Unknown entity type '{0}' in index {1}.",
        //                                                     DatabaseClassName,
        //                                                     Name));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        dbClass = schema.FindDatabaseClass_CaseInsensitive(DatabaseClassName);
        //        if (dbClass == null)
        //        {
        //            throw new Exception(String.Format("Unknown entity type '{0}' in index {1}.",
        //                                                 DatabaseClassName,
        //                                                 Name));
        //        }
        //    }
        //    return dbClass;
        //}
    }
}