// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The primitive system type of this property.</typeparam>
    public abstract class Property<T> : TValue {
#if DEBUG
//		internal string DebugSetter;
//		internal string DebugGetter;
		internal string DebugBoundSetter;
		internal string DebugBoundGetter;
		internal string DebugUnboundSetter;
		internal string DebugUnboundGetter;
#endif

		public readonly Action<Json, T> Setter;
		public readonly Func<Json, T> Getter;
		internal Action<Json, T> BoundSetter;
		internal Func<Json, T> BoundGetter;
		internal Action<Json, T> UnboundSetter;
		internal Func<Json, T>  UnboundGetter;

        internal Func<Json, Property<T>, T, Input<T>> CustomInputEventCreator = null;
        internal List<Action<Json, Input<T>>> CustomInputHandlers = new List<Action<Json, Input<T>>>();

		public Property() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		private T BoundOrUnboundGet(Json json) {
			if (UseBinding(json))
				return BoundGetter(json);
			return UnboundGetter(json);
		}

		private void BoundOrUnboundSet(Json json, T value) {
			if (UseBinding(json))
				BoundSetter(json, value);
			else 
				UnboundSetter(json, value);
		}

		/// <summary>
		/// 
		/// </summary>
		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			dataTypeForBinding = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		internal override bool GenerateBoundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateBoundDelegates<T>(this, json);
			return (BoundGetter != null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		internal override void GenerateUnboundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateUnboundDelegates<T>(this, json, false);
		}

		internal override object GetBoundValueAsObject(Json json) {
			return BoundGetter(json);
		}

		internal override void SetBoundValueAsObject(Json json, object value) {
			BoundSetter(json, (T)value);
		}

        /// <summary>
        /// Adds an inputhandler to this property.
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(
            Func<Json, Property<T>, T, Input<T>> createInputEvent = null,
            Action<Json, Input<T>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class PrimitiveProperty<T> : Property<T> {
		public override bool IsPrimitive {
			get { return true; }
		}
	}
}
