using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Starcounter.Internal.Tests {

    /// <summary>
    /// Implements a set of unit tests and test utility methods to
    /// test the functionality of the <see cref="ErrorCode"/> class.
    /// </summary>
    [TestFixture]
    public class ErrorCodeTest {
        /// <summary>
        /// Allows the execution of an <see cref="Action"/> and asserts
        /// it throws an exception with the Starcounter error code as
        /// specified in <paramref name="code"/>. If the action either
        /// does not raise any exception, or the exception it raises is
        /// not one with the given code, this method fails with an
        /// <see cref="Exception"/> - either the one thrown, or one from
        /// <see cref="Assert.Fail(string)"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        /// <param name="code">The error code we want to assert is tagged
        /// on the exception thrown by the given action.</param>
        public static void AssertThrowsErrorCode(Action action, uint code) {
            AssertThrowsErrorCode(action, new uint[] { code });
        }

        /// <summary>
        /// Allows the execution of an <see cref="Action"/> and asserts
        /// it throws an exception with any of the Starcounter error codes
        /// as specified in <paramref name="codes"/>. If the action either
        /// does not raise any exception, or the exception it raises is
        /// not one found in the array, this method fails with an
        /// <see cref="Exception"/> - either the one thrown, or one from
        /// <see name="Assert.Fail(string)"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        /// <param name="codes">The set of error codes we want to assert is
        /// tagged on the exception thrown by the given action.</param>
        public static void AssertThrowsErrorCode(Action action, uint[] codes) {
            var succeeded = AssertThrowsErrorCodeOrSucceeds(action, codes);
            if (succeeded) {
                Assert.Fail("The action {0} didn't throw any exception", action.ToString());
            }
        }

        /// <summary>
        /// Allows the execution of an <see cref="Action"/> and asserts
        /// it throws an exception with the Starcounter error code as
        /// specified in <paramref name="code"/>, or succeeds. If the
        /// action raise an exception with a code that differs from the
        /// one given, that exception will be rethrown.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        /// <param name="code">The error code we want to assert is tagged
        /// on the exception thrown by the given action.</param>
        /// <returns>True if the action succeeded; false if the action did
        /// in fact raise an exception with the given code.</returns>
        public static bool AssertThrowsErrorCodeOrSucceeds(Action action, uint code) {
            return AssertThrowsErrorCodeOrSucceeds(action, new uint[] { code });
        }

        /// <summary>
        /// Allows the execution of an <see cref="Action"/> and asserts
        /// it throws an exception with a Starcounter error code part of
        /// the <paramref name="codes"/> array, or succeeds. If the
        /// action raise an exception with a code that is not part of the
        /// given array, that exception will be rethrown.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        /// <param name="codes">A set of error code we want to assert is tagged
        /// on the exception thrown by the given action.</param>
        /// <returns>True if the action succeeded; false if the action did
        /// in fact raise an exception with the given code.</returns>
        public static bool AssertThrowsErrorCodeOrSucceeds(Action action, uint[] codes) {
            try {
                action();
                return true;
            } catch (Exception e) {
                uint codeThrown;
                if (!ErrorCode.TryGetCode(e, out codeThrown))
                    throw;

                foreach (var code in codes) {
                    if (codeThrown == code)
                        return false;
                }
                throw;
            }
        }

        /// <summary>
        /// Invokes <see cref="ErrorCode.ToException(uint)"/> with the
        /// given error code, and makes sure <see cref="AssertThrowsErrorCode(Action,uint)"/>
        /// properly catches and process it.
        /// </summary>
        /// <param name="code">The error code to throw, and catch, or 0 if
        /// no exception should be raised, in which case this method should
        /// produce a NUnit failed exception.</param>
        /// <seealso cref="AssertThrowsErrorCode(Action,uint)"/>
        public static void ThrowAndAssertException(uint code) {
            ErrorCodeTest.AssertThrowsErrorCode(() => {
                if (code != 0) {
                    throw ErrorCode.ToException(code);
                }
            }, 
            code);
        }

        /// <summary>
        /// Invokes <see cref="ErrorCode.ToException(uint)"/> with the
        /// given error code, and makes sure <see cref="AssertThrowsErrorCodeOrSucceeds(Action,uint)"/>
        /// properly catches and process it. If the code given is 0,
        /// the exception is not raised, and we should expect that to
        /// be properly handled too.
        /// </summary>
        /// <param name="code">The error code to throw, and catch, or 0 if
        /// no exception should be raised.</param>
        /// <seealso cref="AssertThrowsErrorCodeOrSucceeds(Action,uint)"/>
        public static void ThrowAndAssertExceptionOrSuccess(uint code) {
            ErrorCodeTest.AssertThrowsErrorCodeOrSucceeds(() => {
                if (code != 0) {
                    throw ErrorCode.ToException(code);
                }
            }, 
            code);
        }

        /// <summary>
        /// Performs the most basic tests of <see cref="ErrorCode.ToException(uint)"/>
        /// and the utility methods in this class, allowing exceptions with specified
        /// error codes to be caught in a controlled, predictable manner.
        /// </summary>
        [Test]
        public void TestErrorCodeToException() {
            Assert.DoesNotThrow(() => { ThrowAndAssertException(Error.SCERRUNSPECIFIED); });
            Assert.DoesNotThrow(() => { ThrowAndAssertExceptionOrSuccess(Error.SCERRUNSPECIFIED); });
            Assert.DoesNotThrow(() => { ThrowAndAssertExceptionOrSuccess(0); });
            Assert.Throws<NUnit.Framework.AssertionException>(() => { ThrowAndAssertException(0); });
        }
    }
}
