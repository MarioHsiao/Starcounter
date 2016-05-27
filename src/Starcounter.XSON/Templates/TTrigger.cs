// ***********************************************************************
// <copyright file="TTrigger.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Templates {

    /// <summary>
    /// Represents an action property in a typed json object. An Action property does not contain a value, 
    /// but can be triggered by the client.
    /// </summary>
    /// <remarks>
    /// When you create a Json-by-example file with a null property (i.e. "myfield":null),
    /// the schema template for that property becomes an TTrigger.
    /// </remarks>
    public class TTrigger : TValue {
        private static byte[] jsonValueAsBytes = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

        private Func<Json, TValue, Input> _inputEventCreator;
        private Action<Json, Input> _inputHandler;

        /// <summary>
        /// 
        /// </summary>
        public override bool IsPrimitive {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Type MetadataType {
            get { return typeof(ActionMetadata<Json>); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// </summary>
        /// <value></value>
        public override string JsonType {
            get { return "function"; }
        }

        /// <summary>
        /// </summary>
        /// <value></value>
        public string OnRun { get; set; }

        ///// <summary>
        ///// Creates the default value of this property.
        ///// </summary>
        ///// <param name="parent">The object that will hold the created value (the host Obj or host Array)</param>
        ///// <returns>Always returns false or is it null????? TODO!??</returns>
        //public override object CreateInstance(Json parent) {
        //    return false;
        //}

        /// <summary>
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(Func<Json, TValue, Input> createInputEvent,
                               Action<Json, Input> handler) {
            _inputEventCreator = createInputEvent;
            _inputHandler = handler;
        }

        /// <summary>
        /// Invoking user provided input handler respecting application name.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="input"></param>
        Input InvokeHandler(Json obj) {

            // Setting the application name of the input handler owner.
            String savedAppName = StarcounterEnvironment.AppName;
            try {
                StarcounterEnvironment.AppName = obj.appName;
                return _inputEventCreator.Invoke(obj, this);
            } finally {
                StarcounterEnvironment.AppName = savedAppName;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rawValue"></param>
        public void ProcessInput(Json obj) {
            Input input = null;

            if (_inputEventCreator != null) {
                input = InvokeHandler(obj);
            }

            if (input != null && _inputHandler != null) {
                _inputHandler.Invoke(obj, input);
            } else if (BasedOn != null) {
                // This is an inherited template with no inputhandler, lets 
                // see if the base-template has a registered handler.
                ((TTrigger)BasedOn).ProcessInput(obj);
            }
        }

        internal void ProcessInput(Json obj, Input existingInput) {
            Input input = null;

            if (_inputEventCreator != null) {
                input = InvokeHandler(obj);
            }

            if (input != null && _inputHandler != null) {
                _inputHandler.Invoke(obj, input);
                if (input.Cancelled)
                    existingInput.Cancel();
            }
        }

		internal override void SetDefaultValue(Json parent, bool markAsReplaced = false) {
		}

		internal override void InvalidateBoundGetterAndSetter() {
			base.InvalidateBoundGetterAndSetter();
		}
        
        internal override bool HasBinding() {
            return false;
        }

        internal override bool GenerateBoundGetterAndSetter(Json json) {
			return false;
		}

		internal override void GenerateUnboundGetterAndSetter() {
		}

		internal override void CheckAndSetBoundValue(Json json, bool addToChangeLog) {
		}

		internal override object GetUnboundValueAsObject(Json parent) {
			return null;
		}

		internal override object GetValueAsObject(Json parent) {
			return null;
		}

		internal override void SetValueAsObject(Json parent, object value) {
		}

		internal override void CopyValueDelegates(Template toTemplate) {
		}

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Trigger; }
        }

        internal override Type DefaultInstanceType {
            get { return null; }
        }
    }
}
