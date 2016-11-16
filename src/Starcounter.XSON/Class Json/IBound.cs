
namespace Starcounter {

    /// <summary>
    /// Used to declare the data type od the Data property of 
    /// Json objects described by JSON-by-example in the code-behind
    /// partial class declaration. We don't use generics due to the
    /// limited co-variance support in C#. I.e., in C#, it is not
    /// legal to treat a Json&ltPerson&gt as a Json&ltSpecialPerson&gt unless
    /// Json is an interface.
    /// </summary>
    /// <typeparam name="DataType"></typeparam>
    public interface IBound<out DataType> {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExplicitBound<T> : IBound<T> {
    }
}
