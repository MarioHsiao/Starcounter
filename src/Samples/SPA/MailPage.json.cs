using Starcounter;

partial class MailPage : Json<Email> {
    void Handle(Input.Title input)
    {
        input.Cancel();
    }

    void Handle(Input.Content input)
    {
        input.Cancel();
    }
}