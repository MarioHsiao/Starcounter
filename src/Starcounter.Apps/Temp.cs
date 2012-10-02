
using Starcounter;
public class SqlResult2<T> where T : Entity {

    public T First {
        get {
            return null;
        }
    }
}


public struct Media {
    public HttpResponse Content;

    public static implicit operator Media(string str) {
        return new Media() { Content = new HttpResponse(str) };
    }
    public static implicit operator Media(HttpResponse content) {
        return new Media() { Content = content };
    }
}