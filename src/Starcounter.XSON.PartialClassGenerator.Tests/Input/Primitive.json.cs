namespace Starcounter.XSON.PartialClassGenerator.Tests.Input {
    public partial class Primitive : Json {
        void Handle(Input.IntegerValue input) {
            if (input.Value = 19L)
                input.Cancel();
        }
    }
}
