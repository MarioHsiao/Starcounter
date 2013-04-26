// ***********************************************************************
// <copyright file="ProgressInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Represents the progress of a task, executing to fullfill the
    /// execution of a <see cref="Starcounter.Server.PublicModel.Commands.ServerCommand"/>.
    /// </summary>
    public sealed class ProgressInfo {
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        internal ProgressInfo() {
        }

        /// <summary>
        /// Initializes a new <see cref="ProgressInfo" />.
        /// </summary>
        /// <param name="taskID">The task ID.</param>
        /// <param name="value">The value.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="text">The text.</param>
        /// <remarks>
        /// The <paramref name="value"/> parameter can not have a value
        /// equal to <see cref="int.MinValue"/>. This value is reserved
        /// by the implementation and will result in the raising of a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </remarks>
        internal ProgressInfo(int taskID, int value, int maximum, string text) {
            if (value == int.MinValue) {
                throw new ArgumentOutOfRangeException("value");
            }
            this.TaskIdentity = taskID;
            this.Value = value;
            this.Maximum = maximum;
            this.Text = text;
        }

        /// <summary>
        /// Identity of the well-known command sub-task that has made progress,
        /// or a pseudo number according to protocol.
        /// </summary>
        public int TaskIdentity { 
            get; 
            set; 
        }

        private int _value;
        /// <summary>
        /// Progress value.
        /// </summary>
        /// This property can not be set to a value equal to
        /// <see cref="int.MinValue"/>. This value is reserved
        /// by the implementation and will result in the raising
        /// of a <see cref="ArgumentOutOfRangeException"/>.
        public int Value {
            get { return _value; }
            set {
                if (value == int.MinValue) {
                    throw new ArgumentOutOfRangeException("value");
                }
                _value = value;
            }
        }

        /// <summary>
        /// Progress maximum, if known.
        /// </summary>
        public int Maximum {
            get;
            set;
        }

        /// <summary>
        /// Progress Text.
        /// </summary>
        public string Text {
            get;
            set;
        }

        /// <summary>
        /// Marks this progress as cancelled.
        /// </summary>
        public void Cancel() {
            _value = int.MinValue;
            this.Maximum = _value;
            this.Text = null;
        }

        /// <summary>
        /// Gets a value indicating if the progress has stopped because
        /// the task embracing it was cancelled.
        /// </summary>
        public bool IsCancelled {
            get { return this.Value == int.MinValue; }
        }

        /// <summary>
        /// Gets a value indicating if the current progess record indicates
        /// the task which it representes is completed.
        /// </summary>
        public bool IsCompleted {
            get { return this.Value == this.Maximum; }
        }
    }
}