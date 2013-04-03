// ***********************************************************************
// <copyright file="DatabaseSchema.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Closed set of assemblies and database classes forming a single and consistent database schema.
/// </summary>
[Serializable]
public class DatabaseSchema
{
    private readonly DatabaseAssemblyCollection assemblies;

    readonly Dictionary<String, DatabaseClass> databaseClassesByName = new Dictionary<String, DatabaseClass>();
    readonly Dictionary<String, DatabaseClass> databaseClassesByShortname = new Dictionary<String, DatabaseClass>();
    readonly Dictionary<String, List<DatabaseIndex>> indexByDatabaseClassName = new Dictionary<String, List<DatabaseIndex>>();

    /// <summary>
    /// Initializes a new <see cref="DatabaseSchema"/>.
    /// </summary>
    public DatabaseSchema()
    {
        this.assemblies = new DatabaseAssemblyCollection(this);
    }

    /// <summary>
    /// Gets the collection of assemblies forming the current schema.
    /// </summary>
    public DatabaseAssemblyCollection Assemblies
    {
        get
        {
            return assemblies;
        }
    }

    /// <summary>
    /// </summary>
    public void AddStarcounterAssembly()
    {
        DatabaseAssembly databaseAssembly;

        databaseAssembly = new DatabaseAssembly("Starcounter", System.Reflection.Assembly.GetExecutingAssembly().FullName);
        databaseAssembly.IsCached = true;
        databaseAssembly.SetSchema(this);
        Assemblies.Add(databaseAssembly);
    }

    /// <summary>
    /// Finds a class in the current schema given its name.
    /// </summary>
    /// <param name="name">Name of the requested class.</param>
    /// <returns>The <see cref="DatabaseClass"/> named <paramref name="name"/>, or
    /// <b>null</b> if the schema does not contain a class named <paramref name="name"/>.</returns>
    public DatabaseClass FindDatabaseClass(string name)
    {
        DatabaseClass databaseClass;

        this.databaseClassesByName.TryGetValue(name, out databaseClass);
        return databaseClass;
    }

    /// <summary>
    /// Index a <see cref="DatabaseClass"/> in the current schema.
    /// </summary>
    /// <param name="databaseClass">Class to index.</param>
    /// <remarks>
    /// This method is called when an assembly is added to the schema or when a class
    /// is added to an assembly of this schema. It ensures that all classes in this schema
    ///
    /// </remarks>
    internal void IndexDatabaseClass(DatabaseClass databaseClass)
    {
		Int32 index;
		String shortName = databaseClass.Name;
		index = shortName.LastIndexOf('.');

        shortName = shortName.Substring(index + 1);
        if (this.databaseClassesByShortname.ContainsKey(shortName))
		{
			// Ambiguous type names. Replace value with null.
            this.databaseClassesByShortname[shortName] = null;
		}
		else
		{
            this.databaseClassesByShortname.Add(shortName, databaseClass);
		}

        this.databaseClassesByName.Add(databaseClass.Name, databaseClass);
    }

    /// <summary>
	/// Finds a class in the current schema given its shortname.
	/// </summary>
	/// <param name="name">
	/// Name of the requested class.
	/// </param>
	/// <param name="databaseClass">
	/// Out parameter. contains the <see cref="DatabaseClass"/>, 
	/// or null if not found or on an ambiguous match.
	/// </param>
	/// <returns>
	/// True if found (including ambiguous match), false if no 
	/// class is found.
	/// </returns>
	public Boolean FindDatabaseClassByShortname(string name, out DatabaseClass databaseClass)
	{
        return this.databaseClassesByShortname.TryGetValue(name, out databaseClass);
	}

    /// <summary>
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="fieldDeclaringTypeReflectionName"></param>
    /// <returns></returns>
    public DatabaseAttribute FindDatabaseAttribute(string fieldName, string fieldDeclaringTypeReflectionName)
    {
        DatabaseClass databaseClass = this.FindDatabaseClass(fieldDeclaringTypeReflectionName);
        if (databaseClass == null)
        {
            return null;
        }
        return databaseClass.Attributes[fieldName];
    }

	internal void AddDatabaseIndex(DatabaseIndex index)
	{
		List<DatabaseIndex> indexList;
        if (!indexByDatabaseClassName.TryGetValue(index.DataBaseClass.Name, out indexList))
		{
			indexList = new List<DatabaseIndex>();
            indexByDatabaseClassName.Add(index.DataBaseClass.Name, indexList);
		}
		indexList.Add(index);
    }

    internal DatabaseIndex[] GetIndexesByClassName(String className)
    {
        List<DatabaseIndex> indexList;
        if (indexByDatabaseClassName.TryGetValue(className, out indexList))
        {
            return indexList.ToArray();
        }
        return null;
    }

    /// <summary>
    /// Enumerates all classes contained in the current schema.
    /// </summary>
    /// <returns>An enumerator for all classes contained in the current schema.</returns>
    public IEnumerator<DatabaseClass> EnumerateDatabaseClasses()
    {
        foreach (DatabaseAssembly assembly in assemblies)
        {
            foreach (DatabaseClass databaseClass in assembly.DatabaseClasses)
            {
                yield return databaseClass;
            }
        }
    }

    /// <summary>
    /// Populates a list with the entity classes (different than the root class)
    /// in the current database schema.
    /// </summary>
    /// <param name="classes">Collection to be populated.</param>
    public void PopulateDatabaseEntityClasses(IList<DatabaseEntityClass> classes)
    {
        foreach (DatabaseAssembly assembly in assemblies)
        {
            foreach (DatabaseClass databaseClass in assembly.DatabaseClasses)
            {
                DatabaseEntityClass entityClass = databaseClass as DatabaseEntityClass;
                if (entityClass != null)
                {
                    classes.Add(entityClass);
                }
            }
        }
    }

    /// <summary>
    /// Populates a list (ordered collection) with the database classes present in the
    /// database schema, sorted by order of inheritance (if A is the parent of B, A is
    /// before B in the list).
    /// </summary>
    /// <param name="list">The list to be populate.</param>
    public void PopulateOrderedDatabaseClasses(IList<DatabaseClass> list)
    {
        Dictionary<DatabaseClass, object> index = new Dictionary<DatabaseClass, object>(this.databaseClassesByName.Count);
        foreach (DatabaseClass databaseClass in this.databaseClassesByName.Values)
        {
            RecursivePopulateOrderedClasses(databaseClass, list, index);
        }
    }

    /// <summary>
    /// Populates a list (ordered collection) with the database classes present in the
    /// database schema, sorted by order of inheritance (if A is the parent of B, A is
    /// before B in the list).
    ///
    /// Excludes extension classes.
    /// </summary>
    /// <param name="list">The list to be populate.</param>
    public void PopulateOrderedDatabaseEntityClasses(IList<DatabaseClass> list)
    {
        Dictionary<DatabaseClass, object> index = new Dictionary<DatabaseClass, object>(this.databaseClassesByName.Count);
        foreach (DatabaseClass databaseClass in this.databaseClassesByName.Values)
        {
            if (databaseClass is DatabaseEntityClass)
            {
                RecursivePopulateOrderedClasses(databaseClass, list, index);
            }
        }
    }

    /// <summary>
    /// </summary>
    public void PopulateOrderedDatabaseEntityClasses2(IList<DatabaseEntityClass> list) {
        Dictionary<DatabaseEntityClass, object> index = new Dictionary<DatabaseEntityClass, object>(this.databaseClassesByName.Count);
        foreach (DatabaseEntityClass databaseClass in this.databaseClassesByName.Values) {
            DatabaseEntityClass entityClass = databaseClass as DatabaseEntityClass;
            if (entityClass != null) {
                RecursivePopulateOrderedEntityClasses(entityClass, list, index);
            }
        }
    }

    /// <summary>
    /// Formats the current assembly and all its members to a writer.
    /// </summary>
    /// <param name="writer">The writer to which the object should be formatted.</param>
    public void DebugOutput(IndentedTextWriter writer)
    {
        foreach (DatabaseAssembly assembly in assemblies)
        {
            assembly.DebugOutput(writer);
            writer.WriteLine();
        }
    }

    /// <summary>
    /// Asserts all assemblies are properly initialized in the domain
    /// they live. Should be invoked every time the schema has been
    /// deserialized in a new AppDomain (i.e either after it was fully
    /// assembled from disk .schema files or after it was serialized
    /// accross an AppDomain boundary.
    /// </summary>
    public void AfterDeserialization()
    {
        // Set the references to the DatabaseSchema in DatabaseAssemblies, since
        // they have not been serialized.
        
        foreach (DatabaseAssembly assembly in assemblies)
        {
            assembly.SetSchema(this);
        }
        
        foreach (DatabaseAssembly assembly in assemblies)
        {
            assembly.OnSchemaComplete();
        }
    }

    /// <summary>
    /// Inserts a given class and its ancestors in a collection of classes that should be
    /// ordered by inheritance (parent first, children after).
    /// </summary>
    /// <param name="databaseClass">The database class that has to be inserted (as well
    /// as its ancestors) to the collection.</param>
    /// <param name="orderedClasses">The collection into which the classes have to
    /// be added.</param>
    /// <param name="index">Index of classes that are already present in <paramref name="orderedClasses"/>.
    /// </param>
    private static void RecursivePopulateOrderedClasses(
        DatabaseClass databaseClass,
        IList<DatabaseClass> orderedClasses,
        Dictionary<DatabaseClass, object> index)
    {
        if (!index.ContainsKey(databaseClass))
        {
            if (databaseClass.BaseClass != null)
            {
                RecursivePopulateOrderedClasses(databaseClass.BaseClass, orderedClasses, index);
            }
            orderedClasses.Add(databaseClass);
            index.Add(databaseClass, null);
        }
    }

    private static void RecursivePopulateOrderedEntityClasses(
        DatabaseEntityClass databaseClass,
        IList<DatabaseEntityClass> orderedClasses,
        Dictionary<DatabaseEntityClass, object> index) {
        if (!index.ContainsKey(databaseClass)) {
            DatabaseEntityClass databaseClassBase = databaseClass.BaseClass as DatabaseEntityClass;
            if (databaseClassBase != null) {
                RecursivePopulateOrderedEntityClasses(databaseClassBase, orderedClasses, index);
            }
            orderedClasses.Add(databaseClass);
            index.Add(databaseClass, null);
        }
    }
}
}