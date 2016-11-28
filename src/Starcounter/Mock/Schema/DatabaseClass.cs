
using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sc.Server.Weaver.Schema
{
    /// <summary>
    /// Represents any class whose instances are to be stored in the database.
    /// </summary>
    [Serializable]
    public abstract partial class DatabaseClass : DatabaseSchemaElement, IDatabaseAttributeType
    {
        private readonly DatabaseAssembly assembly;
        private readonly string name;
        private readonly DatabaseAttributeCollection attributes = new DatabaseAttributeCollection();
        private DatabaseClassRef baseClass;
        private Boolean internalMetadataClass;
        private bool isQualifiedOnlyByFullNameInQueries;

        /// <summary>
        /// Initializes a new <see cref="DatabaseClass"/>.
        /// </summary>
        /// <param name="assembly">Assembly to which the class belong.</param>
        /// <param name="name">Full name of the class.</param>
        protected DatabaseClass(DatabaseAssembly assembly, string name)
        {
            this.assembly = assembly;
            this.name = name;
        }

        /// <summary>
        /// Initializes a new <see cref="DatabaseClass"/>.
        /// </summary>
        /// <param name="assembly">Assembly to which the class belong.</param>
        /// <param name="name">Full name of the class.</param>
        /// <param name="internalMetadataClass">
        /// If true this class is an internal class for starcounter that should be handled a bit different.
        /// for example it should be filtered out when doing an unload.
        /// </param>
        protected DatabaseClass(DatabaseAssembly assembly, string name, Boolean internalMetadataClass)
        {
            this.assembly = assembly;
            this.name = name;
            this.internalMetadataClass = internalMetadataClass;
        }

        internal Boolean IsInternalMetadataClass {
            get { return internalMetadataClass; }
        }

        /// <summary>
        /// Gets the assembly to which the class belong.
        /// </summary>
        public DatabaseAssembly Assembly {
            get {
                return this.assembly;
            }
        }

        /// <summary>
        /// Gets or sets the base class.
        /// </summary>
        /// <remarks>
        /// A <see cref="DatabaseClass"/>, or <b>null</b> if the base class
        /// is the <b>Entity</b> class.
        /// </remarks>
        public DatabaseClass BaseClass {
            get {
                return DatabaseClassRef.Resolve(baseClass, this);
            }
            set {
                baseClass = DatabaseClassRef.MakeRef(value);
            }
        }


        /// <summary>
        /// Gets or sets the class name, including the namespace.
        /// </summary>
        public string Name {
            get {
                return name;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating if the current type should be qualified
        /// only by its full name.
        /// </summary>
        public bool IsQualifiedOnlyByFullNameInQueries {
            get {
                return isQualifiedOnlyByFullNameInQueries;
            }
            set {
                isQualifiedOnlyByFullNameInQueries = value;
            }
        }

        /// <summary>
        /// Gets the collection of database attributes defined in the current class.
        /// </summary>
        public DatabaseAttributeCollection Attributes {
            get {
                return this.attributes;
            }
        }

        /// <summary>
        /// Searches an attribute by name in the current class and in all ancestors.
        /// </summary>
        /// <param name="name">Name of the searched field.</param>
        /// <returns>The <see cref="DatabaseAttribute"/> named <paramref name="name"/>, or <b>null</b>
        /// if the type and its ancestors don't contain a field with that name.</returns>
        public DatabaseAttribute FindAttributeInAncestors(string name)
        {
            if (this.attributes.Contains(name))
            {
                return this.attributes[name];
            }
            if (this.baseClass != null)
            {
                return this.BaseClass.FindAttributeInAncestors(name);
            }
            return null;
        }

        /// <summary>
        /// Searches an attribute by name in the current class and in all ancestors, using
        /// a specified predicate to determine if attributes are considered matches or not.
        /// </summary>
        /// <param name="predicate">The predicate that determine if an attribute match.</param>
        /// <returns>An attribute if found; null if not.</returns>
        public DatabaseAttribute FindAttributeInAncestors(Func<DatabaseAttribute, bool> predicate)
        {
            var result = this.attributes.FirstOrDefault(predicate);
            if (result == null)
            {
                if (baseClass != null)
                {
                    return BaseClass.FindAttributeInAncestors(predicate);
                }
            }
            return result;
        }

        //PI110503
        //// Added to support case insensitivity.
        //public DatabaseAttribute FindAttributeInAncestors_CaseInsensitive(string name)
        //{
        //    String nameOriginalCase = null;
        //    if (this.attributes.Contains_CaseInsensitive(name, out nameOriginalCase))
        //    {
        //        return this.attributes[nameOriginalCase];
        //    }
        //    if (this.baseClass != null)
        //    {
        //        return this.BaseClass.FindAttributeInAncestors_CaseInsensitive(name);
        //    }
        //    return null;
        //}

        /// <summary>
        /// Gets the schema to which the current class belong.
        /// </summary>
        public override DatabaseSchema Schema {
            get {
                return this.assembly.Schema;
            }
        }

        internal virtual void OnSchemaComplete()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0} {1}",
                                 this.GetType().Name,
                                 this.name);
        }

        /// <summary>
        /// Formats the current assembly and all its members to a writer.
        /// </summary>
        /// <param name="writer">The writer to which the object should be formatted.</param>
        public virtual void DebugOutput(IndentedTextWriter writer)
        {
            writer.WriteLine(this.ToString());
            if (this.attributes.Count > 0)
            {
                writer.Indent++;
                writer.WriteLine("Base: {0}", this.baseClass);
                writer.WriteLine("Attributes:");
                writer.Indent++;
                foreach (DatabaseAttribute attribute in this.attributes)
                {
                    writer.WriteLine(attribute.ToString());
                }
                writer.Indent--;
                writer.Indent--;
            }
        }


    }

    /// <summary>
    /// Collection of database classes (<see cref="DatabaseClass"/>).
    /// </summary>
    /// <remarks>
    /// This collection has the particularity to index classes by name and
    /// and to update the schema-level index of classes.
    /// </remarks>
    [Serializable]
    public class DatabaseClassCollection : KeyedCollection<string, DatabaseClass>
    {
        private readonly DatabaseAssembly assembly;

        /// <summary>
        /// Initializes a new <see cref="DatabaseClassCollection"/>.
        /// </summary>
        /// <param name="assembly">Assembly to which the collection belong.</param>
        internal DatabaseClassCollection(DatabaseAssembly assembly)
        {
            this.assembly = assembly;
        }


        /// <summary>
        /// Gets the class name.
        /// </summary>
        /// <param name="item">The database class.</param>
        /// <returns>The database class name.</returns>
        protected override string GetKeyForItem(DatabaseClass item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                throw new InvalidOperationException("item.Name is not set.");
            }
            return item.Name;
        }

        /// <summary>
        /// Called when an item is inserted. We update the index located in the database schema.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="item">Item.</param>
        protected override void InsertItem(int index, DatabaseClass item)
        {
            base.InsertItem(index, item);
            this.assembly.Schema.IndexDatabaseClass(item);
        }

        /// <summary>
        /// The <b>Remove</b> operation is not supported.
        /// </summary>
        /// <param name="index"></param>
        protected override void RemoveItem(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The <b>Set</b> operation is not supported.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        protected override void SetItem(int index, DatabaseClass item)
        {
            throw new NotSupportedException();
        }
    }
}
