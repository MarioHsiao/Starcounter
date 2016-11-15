
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sc.Server.Weaver.Schema
{
    /// <summary>
    /// Represents an assembly in the database schema.
    /// </summary>
    /// <remarks>
    /// The database server has no notion of assembly. Type names for the database
    /// server does not contain the assembly name. This concept exists for the database
    /// schema so that it is possible to save the part of the database schema that
    /// is defined by a single assembly, so that it can be loaded later without
    /// requiring the complete assembly to be reloaded.
    /// </remarks>
    [Serializable]
    public sealed class DatabaseAssembly : DatabaseSchemaElement
    {
        // The link to the schema is not serialized, so that assemblies can
        // be serialized separately.
        [NonSerialized]
        private DatabaseSchema schema;
        private readonly string name;
        private readonly string fullName;
        private readonly DatabaseClassCollection databaseClasses;
        private bool isTransformed;
        private bool hasDebuggingSymbols;
        private readonly Dictionary<string, string> dependencies = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new <see cref="DatabaseAssembly" />.
        /// </summary>
        /// <param name="name">Assembly name.</param>
        /// <param name="fullName">The full name.</param>
        public DatabaseAssembly(string name, string fullName)
        {
            this.name = name;
            this.fullName = fullName;
            this.databaseClasses = new DatabaseClassCollection(this);
        }

        /// <summary>
        /// Gets or sets the schema to which the current assembly belongs.
        /// </summary>
        public override DatabaseSchema Schema {
            get {
                return this.schema;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="schema"></param>
        public void SetSchema(DatabaseSchema schema)
        {
            this.schema = schema;
        }

        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Gets the assembly full name.
        /// </summary>
        public string FullName { get { return fullName; } }

        /// <summary>
        /// Gets the collection of database classes contained in this assembly.
        /// </summary>
        /// <remarks>
        /// This collection contains all database classes without exception.
        /// Consumer code should filter them if it is only interested by entity classes,
        /// for instance.
        /// </remarks>
        public DatabaseClassCollection DatabaseClasses {
            get {
                return this.databaseClasses;
            }
        }

        /// <summary>
        /// Determines whether the current assembly is transformed
        /// (user assemblies are transformed, system assemblies are typically not).
        /// </summary>
        public bool IsTransformed {
            get {
                return isTransformed;
            }
            set {
                isTransformed = value;
            }
        }

        /// <summary>
        /// Determines whether the current assembly has debugging symbols
        /// (a PDB file).
        /// </summary>
        public bool HasDebuggingSymbols {
            get {
                return hasDebuggingSymbols;
            }
            set {
                hasDebuggingSymbols = value;
            }
        }

        /// <summary>
        /// Gets the dictionary of dependent assemblies.
        /// </summary>
        /// <remarks>
        /// The item key is the assembly name (without file extension). The item value
        /// is the hash value coded in hexadecimal.
        /// </remarks>
        public Dictionary<string, string> Dependencies {
            get {
                return dependencies;
            }
        }

        /// <summary>
        /// Formats the current assembly and all its members to a writer.
        /// </summary>
        /// <param name="writer">The writer to which the object should be formatted.</param>
        public void DebugOutput(IndentedTextWriter writer)
        {
            writer.WriteLine("Assembly {0}", this.name);
            writer.Indent++;
            writer.WriteLine();
            foreach (KeyValuePair<string, string> dependency in this.Dependencies)
            {
                writer.WriteLine("Dependency: {0}: {1}", dependency.Key, dependency.Value);
            }
            writer.WriteLine();
            foreach (DatabaseClass databaseClass in this.databaseClasses)
            {
                databaseClass.DebugOutput(writer);
                writer.WriteLine();
            }
            writer.Indent--;
        }

        /// <summary>
        /// Serializes the current assembly to a file.
        /// </summary>
        /// <param name="fileName">Name of the file to which the assembly should be serialized.</param>
        public void Serialize(string fileName)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = File.Create(fileName))
            {
                formatter.Serialize(stream, this);
            }
        }

        /// <summary>
        /// Deserializes a file representing a <see cref="DatabaseAssembly"/>.
        /// </summary>
        /// <param name="fileName">Name of the file containing the serialized <see cref="DatabaseAssembly"/>.</param>
        /// <returns>The <see cref="DatabaseAssembly"/> stored in <paramref name="fileName"/>.</returns>
        public static DatabaseAssembly Deserialize(string fileName)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = File.OpenRead(fileName))
            {
                return (DatabaseAssembly)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Deserializes a file representing a <see cref="DatabaseAssembly"/>.
        /// </summary>
        /// <param name="stream">A stream containing the serialized <see cref="DatabaseAssembly"/>.</param>
        /// <returns>The <see cref="DatabaseAssembly"/> stored in <paramref name="stream"/>.</returns>
        public static DatabaseAssembly Deserialize(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (DatabaseAssembly)formatter.Deserialize(stream);
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnSchemaComplete()
        {
            foreach (DatabaseClass databaseClass in databaseClasses)
            {
                databaseClass.OnSchemaComplete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Assembly " + this.name;
        }
    }

    /// <summary>
    /// Collection of <see cref="DatabaseAssembly"/> with indexing by name.
    /// </summary>
    /// <remarks>
    /// This collection may only be owned by a <see cref="DatabaseSchema"/>. When an assembly
    /// is added to the collection, the database classes it contains are automatically indexed
    /// in the schema.
    /// </remarks>
    [Serializable]
    public class DatabaseAssemblyCollection : ICollection<DatabaseAssembly>
    {
        private readonly Dictionary<string, DatabaseAssembly> dictionary =
            new Dictionary<string, DatabaseAssembly>(32, StringComparer.InvariantCultureIgnoreCase);
        private readonly DatabaseSchema schema;

        /// <summary>
        /// Initializes a new <see cref="DatabaseAssemblyCollection"/>.
        /// </summary>
        /// <param name="schema"><see cref="DatabaseSchema"/> to which the new collection belongs</param>
        internal DatabaseAssemblyCollection(DatabaseSchema schema)
        {
            this.schema = schema;
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(DatabaseAssembly item)
        {
            item.SetSchema(this.schema);
            // Index all types in this assembly.
            foreach (DatabaseClass databaseClass in item.DatabaseClasses)
            {
                this.schema.IndexDatabaseClass(databaseClass);
            }
            this.dictionary.Add(item.Name, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(DatabaseAssembly item)
        {
            return this.dictionary.ContainsKey(item.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(DatabaseAssembly[] array, int arrayIndex)
        {
            this.dictionary.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(DatabaseAssembly item)
        {
            return this.dictionary.Remove(item.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count {
            get {
                return this.dictionary.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly {
            get {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<DatabaseAssembly> GetEnumerator()
        {
            return this.dictionary.Values.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DatabaseAssembly this[string name] {
            get {
                return this.dictionary[name];
            }
        }
    }
}