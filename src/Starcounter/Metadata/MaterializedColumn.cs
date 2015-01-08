// ***********************************************************************
// <copyright file="MaterializedColumn.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System.Reflection;

namespace Starcounter.Internal.Metadata {

    /// <summary>
    /// </summary>
    public sealed class MaterializedColumn : SystemEntity {
        #region Infrastructure, reflecting what is emitted by the weaver.
#pragma warning disable 0649, 0169
        internal sealed class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_NameToken;
            internal static int columnHandle_Table;
            internal static int columnHandle_Index;
            internal static int columnHandle_Name;
            internal static int columnHandle_PrimitiveType;
            internal static int columnHandle_AlwaysUnique;
            internal static int columnHandle_Nullable;
            internal static int columnHandle_Inherited;
        }
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the database binding <see cref="TypeDef"/> representing
        /// the type in the database and holding its table- and column defintions.
        /// </summary>
        /// <remarks>
        /// Developer note: if you extend or change this class in any way, make
        /// sure to keep the <see cref="MaterializedColumn.__starcounterTypeSpecification"/>
        /// class in sync with what is returned by this method.
        /// </remarks>
        /// <returns>A <see cref="TypeDef"/> representing the current
        /// type.</returns>
        static internal TypeDef CreateTypeDef() {
            return MetadataBindingHelper.CreateTypeTableDef(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        /// <inheritdoc />
        public MaterializedColumn(Uninitialized u)
            : base(u) {
        }

        /// <summary>
        /// </summary>
        public ulong NameToken {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_NameToken); }
        }

        /// <summary>
        /// </summary>
        public MaterializedTable Table {
            get { return (MaterializedTable)DbState.ReadObject(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Table); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Index); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name); }
        }

        /// <summary>
        /// </summary>
        public ulong PrimitiveType {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_PrimitiveType); }
        }

        /// <summary>
        /// </summary>
        public bool AlwaysUnique {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_AlwaysUnique); }
        }

        /// <summary>
        /// </summary>
        public bool Nullable {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Nullable); }
        }

        /// <summary>
        /// </summary>
        public bool Inherited {
            get { return DbState.ReadBoolean(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Inherited); }
        }
    }
}
