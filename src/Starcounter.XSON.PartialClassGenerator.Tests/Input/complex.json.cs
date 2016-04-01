using System;
using MySampleNamespace.Something;
using SomeOtherNamespace;

[Temp_json]
public class ClassWithoutNamespace : Page {
    static void Main() {
        using (string s = "") {

        }

        Handle.POST("/init-demo-data", () => {
            return 201;
        });
    }
}

namespace MySampleNamespace {
    namespace WrongNamespace {
        [Test]
        public class WrongClass {
            public string Apa;

            public void GetApa() {
                return Apa;
            }
        }
    }

    [Complex_json]
    partial class Complex : MyBaseJsonClass, ISomeInterface, IBound<Order> {
        static Complex() {
            // Some comment inside a method.
            string Complex = "";
        }

        public static Complex Create<T>(Json employer, Func<T, ModalResult> action) where T : ComplexData, new() {
            return Create<T>(employer, action, (ModalResult result) => {
                if (result.Value == ModalResultType.SUCCESS) {
                    if (result.Msg.Length > 0) {
                        Master.SendUserInfo(employer, result.Msg);
                    } else {
                        Master.SendUserInfo(employer, "Success");
                    }
                } else {
                    Master.SendUserError(employer, result.Msg);
                }
            });
        }

        public static Complex Create<T>(Json employer, Func<T, ModalResult> action, Func<ModalResult> apa) where T : ComplexData, new() {
            return new Complex();
        }

        [Random5]
        public int Foo123;

        [Obsolete, NonSerialized]
        public void Handle(Input.userLink input) {
        }

        public void Handle(Input.child.test input) {
        }

        #region Test of several inputhandler registration and sortorder.
        public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input) {
        }

        [Another(Fake = true), Test]
        [Complex_json.ActivePage.SubPage1.SubPage2.SubPage3]
        [SomeOther]
        [Random2][Random8][Random9]
        public partial class SubPage3Impl : Json, IFoo, IFoo3, IBound<OrderItem> {
            [Complex_json.Blabla.bla]
            public partial class SubPage3Sub1 : Json {
            }

            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input) {
            }
        }

        /// <summary>
        /// Testing comments in code for the analyzer.
        /// </summary>
        [Complex_json.ActivePage]
        public partial class ActivePageImpl : Json, IFoo {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input) {
            }
        }

        [Complex_json.ActivePage.SubPage1.SubPage2]
        public partial class SubPage2Impl : Json {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input) {
            }
        }

        [Complex_json.ActivePage.SubPage1]
        public partial class SubPage1Impl : Json {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input) {
            }
        }
        #endregion
    }
}
