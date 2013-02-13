// ***********************************************************************
// <copyright file="IApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// An App is a live view model object controlled by your C# application code.
    /// An App object is modelled to simulate a JSON object. The App object can have properties such as strings, booleans
    /// arrays and other app objects. In this way you can build JSON like trees. These trees are then bound to your GUI.
    /// Whenever you change properties in the tree, such as changing values or adding and removing elements in the arrays,
    /// the UI gets updated. Likewise, when the user clicks or writes text inside your UI, the App view model tree gets updated.
    /// This is a very efficient way to connect a user interface to your application logic and will result in clean, simple
    /// and easy to understand and maintain code. This model view controller pattern (MVC) pattern is sometimes referred to as
    /// MVVM (model view view-model) or MDV (model driven views).
    /// </summary>
    /// <remarks>An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).</remarks>
    public interface IApp : IContainer {


#if !SERVERSIDE
        object View { get; set; }
#endif
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Object.</returns>
        object GetValue(OldIValueTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIValueTemplate property, object value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool GetValue( OldIBoolTemplate property );
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        void SetValue( OldIBoolTemplate property, bool value );

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Decimal.</returns>
        decimal GetValue(OldIDecimalTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIDecimalTemplate property, decimal value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Double.</returns>
        double GetValue(OldIDoubleTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIDoubleTemplate property, double value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Int32.</returns>
        int GetValue(OldIIntTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIIntTemplate property, int value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>UInt64.</returns>
        UInt64 GetValue(OldIOidTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIOidTemplate property, UInt64 value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.String.</returns>
        string GetValue(OldIStringTemplate property);
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetValue(OldIStringTemplate property, string value);

    }
}
