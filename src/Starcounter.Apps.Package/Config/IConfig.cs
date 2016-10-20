namespace Starcounter.Apps.Package.Config {
    public interface IConfig {
        string GetString();
        string ID { get; set; }
        string Channel { get; set; }
        string Version { get; set; }
    }
}
