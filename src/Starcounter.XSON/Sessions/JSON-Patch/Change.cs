
using Starcounter.Templates;
using System;
namespace Starcounter.Internal.XSON {

    /// <summary>
    /// A change of either a value, added or removed item in an json-tree.
    /// </summary>
    public struct Change {
//        public const byte UNDEFINED = 0;
        public const byte REMOVE = 1;
        public const byte REPLACE = 2;
        public const byte ADD = 3;

 //       internal static Change Null = new Change(UNDEFINED, null, null, -1);

        /// <summary>
        /// The type of change.
        /// </summary>
        public readonly byte ChangeType;

        /// <summary>
        /// The object that was changed.
        /// </summary>
        public readonly Json Obj;

        /// <summary>
        /// The template of the property that was changed.
        /// </summary>
        public readonly TValue Property;

        /// <summary>
        /// The index if the change is add or remove. Will be
        /// -1 in other cases.
        /// </summary>
        public readonly Int32 Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Change" /> struct.
        /// </summary>
        /// <param name="changeType">The change type.</param>
        /// <param name="app">The app that was changed.</param>
        /// <param name="prop">The template of the property that was changed.</param>
        /// <param name="index">The index.</param>
        private Change(byte changeType, Json obj, TValue prop, Int32 index) {
#if DEBUG
			if (prop != null)
				obj.Template.VerifyProperty(prop);
#endif
            ChangeType = changeType;
            Obj = obj;
            Property = prop;
            Index = index;
        }

        /// <summary>
        /// Returns true if this change is a change of the same app and template.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        internal Boolean IsChangeOf(Json obj, TValue template) {
            return (Obj == obj && Property == template);
        }

        /// <summary>
        /// Creates and returns an instance of an Add change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was added.</param>
        /// <param name="index">The index in the list of the added item.</param>
        /// <returns></returns>
        internal static Change Add(Json obj, TObjArr list, Int32 index) {
            return new Change(Change.ADD, obj, list, index);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="tobj"></param>
		/// <returns></returns>
		internal static Change Add(Json obj) {
			return new Change(Change.REPLACE, obj, null, -1);
		}

        /// <summary>
        /// Creates and returns an instance of a Remove change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was removed.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        /// <returns></returns>
        internal static Change Remove(Json obj, TObjArr list, Int32 index) {
            return new Change(Change.REMOVE, obj, list, index);
        }

        /// <summary>
        /// Creates and returns an instance of an Update change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The template of the property that was updated.</param>
        /// <returns></returns>
        internal static Change Update(Json obj, TValue property) {
            return new Change(Change.REPLACE, obj, property, -1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static Change Update(Json obj, TValue property, int index) {
            return new Change(Change.REPLACE, obj, property, index);
        }
    }
}