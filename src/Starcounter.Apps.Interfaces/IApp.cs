

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
    /// <remarks>
    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change). 
    /// </remarks>
    public interface IApp : IAppNode {


#if !SERVERSIDE
        object View { get; set; }
#endif
        object GetValue(IValueTemplate property);
        void SetValue(IValueTemplate property, object value);

        bool GetValue( IBoolTemplate property );
        void SetValue( IBoolTemplate property, bool value );

        decimal GetValue(IDecimalTemplate property);
        void SetValue(IDecimalTemplate property, decimal value);

        double GetValue(IDoubleTemplate property);
        void SetValue(IDoubleTemplate property, double value);

        int GetValue(IIntTemplate property);
        void SetValue(IIntTemplate property, int value);

        UInt64 GetValue(IOidTemplate property);
        void SetValue(IOidTemplate property, UInt64 value);

        string GetValue(IStringTemplate property);
        void SetValue(IStringTemplate property, string value);

    }
}
