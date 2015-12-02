namespace Starcounter.Ioc {

    /// <summary>
    /// Defines simplest possible interface of a service container
    /// retreival provider, allowing installed services to be got.
    /// </summary>
    public interface IServices {
        T Get<T>();
    }
}