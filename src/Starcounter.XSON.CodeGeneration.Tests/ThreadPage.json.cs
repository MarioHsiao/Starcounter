using Starcounter;

partial class ThreadPage : Json<Thread> {

    [ThreadPage.json.Mails]
    partial class MailEntry : MailPage {
    }
}
