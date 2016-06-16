
using Starcounter.Templates;
using System;
namespace Starcounter.Internal.XSON {

    /// <summary>
    /// A change of either a value, added or removed item in an json-tree.
    /// </summary>
    public struct Change {
        public const byte INVALID = 0;
        public const byte REMOVE = 1;
        public const byte REPLACE = 2;
        public const byte ADD = 3;
        public const byte MOVE = 4;

        public static Change Invalid = new Change(Change.INVALID, null, null, -1, null, -1, false);

        /// <summary>
        /// The type of change.
        /// </summary>
        public readonly byte ChangeType;

        /// <summary>
        /// The parent of the property that was changed.
        /// </summary>
        public readonly Json Parent;

        /// <summary>
        /// The template of the property that was changed.
        /// </summary>
        public readonly TValue Property;

        /// <summary>
        /// The index if the change is add or remove. Will be -1 in other cases.
        /// </summary>
        public Int32 Index;

        /// <summary>
        /// If the change is move, this value will be the old index in the array
        /// </summary>
        public Int32 FromIndex;

        /// <summary>
        /// If the change is for an item in an array this will be the item, otherwise it will be null.
        /// </summary>
        public readonly Json Item;

        /// <summary>
        /// 
        /// </summary>
        public bool SuppressNamespace;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Change" /> struct.
        /// </summary>
        /// <param name="changeType">The change type.</param>
        /// <param name="app">The app that was changed.</param>
        /// <param name="prop">The template of the property that was changed.</param>
        /// <param name="index">The index.</param>
        private Change(byte changeType, Json obj, TValue prop, Int32 index, Json item, Int32 fromIndex = -1, bool suppressNS = false) {
            ChangeType = changeType;
            Parent = obj;
            Property = prop;
            Index = index;
            Item = item;
            FromIndex = fromIndex;
            SuppressNamespace = suppressNS;

#if DEBUG
            if (prop != null)
                Parent.Template.VerifyProperty(prop);
#endif
        }

        ///// <summary>
        ///// Returns true if this change is a change of the same app and template.
        ///// </summary>
        ///// <param name="app">The app.</param>
        ///// <param name="template">The template.</param>
        //internal Boolean IsChangeOf(Json obj, TValue template) {
        //    return (Obj == obj && Property == template);
        //}

        /// <summary>
        /// Creates and returns an instance of an Add change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was added.</param>
        /// <param name="index">The index in the list of the added item.</param>
        /// <returns></returns>
        internal static Change Add(Json obj, TObjArr list, Int32 index, Json item) {
            return new Change(Change.ADD, obj, list, index, item);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tobj"></param>
		/// <returns></returns>
		internal static Change Add(Json obj) {
			return new Change(Change.REPLACE, obj, null, -1, null);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="list"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static Change Move(Json parent, TObjArr list, Int32 fromIndex, Int32 toIndex, Json item) {
            return new Change(Change.MOVE, parent, list, toIndex, item, fromIndex);
        }

        /// <summary>
        /// Creates and returns an instance of a Remove change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was removed.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        /// <returns></returns>
        internal static Change Remove(Json obj, TObjArr list, Int32 index, Json item) {
            return new Change(Change.REMOVE, obj, list, index, item);
        }

        /// <summary>
        /// Creates and returns an instance of an Update change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The template of the property that was updated.</param>
        /// <returns></returns>
        internal static Change Update(Json obj, TValue property) {
            return new Change(Change.REPLACE, obj, property, -1, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static Change Update(Json obj, TValue property, int index, Json item) {
            return new Change(Change.REPLACE, obj, property, index, item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static Change Update(Json obj, TValue property, bool suppressNamespace) {
            return new Change(Change.REPLACE, obj, property, -1, null, -1, suppressNamespace);
        }
    }
}