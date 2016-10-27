namespace Starcounter.Weaver
{
    /// <summary>
    /// Defines the semantics of a weaver host.
    /// </summary>
    public interface IWeaverHost
    {
        void OnWeaverSetup(WeaverSetup setup);
        void OnWeaverStart();
        void OnWeaverDone(bool result);

        void WriteDebug(string message, params object[] parameters);
        void WriteInformation(string message, params object[] parameters);
        void WriteWarning(string message, params object[] parameters);
        void WriteError(uint code, string message, params object[] parameters);
    }
}