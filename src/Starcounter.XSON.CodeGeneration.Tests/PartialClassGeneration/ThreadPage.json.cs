using Starcounter;

[ThreadPage_json]
partial class ThreadPage : Json<Thread> {

    [ThreadPage_json.Mails]
    partial class MailEntry : MailPage {
    }
}
