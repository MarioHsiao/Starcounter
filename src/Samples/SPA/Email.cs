using Starcounter;

[Database]
public class Email {
    public string Id;
    public string Title;
    public string Content;
    public string Uri
    {
        get { return "/emails/" + Id; }
    }
}
