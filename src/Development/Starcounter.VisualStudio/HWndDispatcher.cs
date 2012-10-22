using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Starcounter.VisualStudio {

    internal static class HWndDispatcher {
        private static IntPtr hHook;
        private static int messageCode;
        private static readonly Queue<AsyncAction> actions = new Queue<AsyncAction>();

        private static GetMsgProc onGetMessage;

        #region P-Invoke

        private delegate IntPtr GetMsgProc(int code, UIntPtr wParam, IntPtr lParam);

        private const int WH_GETMESSAGE = 3;
        private const int HC_ACTION = 0;

#pragma warning disable 0649

        private struct MSG {
            public IntPtr hwnd;
            public int message;
            public UIntPtr wParam;
            public UIntPtr lParam;
            public uint time;
            public POINT pt;
        }

        private struct POINT {
            public int X;
            public int Y;
        }

#pragma warning restore 0649

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, GetMsgProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int PostMessage(IntPtr hWnd, int nCode, UIntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        #endregion

        public static void Initialize() {
            if (messageCode == 0) {
                messageCode = RegisterWindowMessage(typeof(HWndDispatcher).AssemblyQualifiedName);
                if (messageCode < 0xC000 || messageCode > 0xFFFF) {
                    throw new ApplicationException(string.Format("Cannot register a window message: error {0}.",
                                                                 Marshal.GetLastWin32Error()));
                }
            }
            if (hHook == IntPtr.Zero) {
                // Keep a reference of the delegate so that it does not get collected.
                onGetMessage = OnGetMessage;
                hHook = SetWindowsHookEx(WH_GETMESSAGE, onGetMessage, IntPtr.Zero, GetCurrentThreadId());
            }
        }


        public static IAsyncResult BeginInvoke(IntPtr hWnd, Action action) {
            // Make sure the class is initialized.
            if (hHook == IntPtr.Zero) {
                throw new InvalidOperationException("HWndDispatcher has not been initialized.");
            }
            AsyncAction asyncAction = new AsyncAction(action);
            lock (actions) actions.Enqueue(asyncAction);
            PostMessage(hWnd, messageCode, UIntPtr.Zero, IntPtr.Zero);
            return asyncAction;
        }

        public static void EndInvoke(IAsyncResult asyncResult) {
            AsyncAction asyncAction = (AsyncAction)asyncResult;
            if (!asyncAction.IsCompleted) {
                asyncAction.AsyncWaitHandle.WaitOne();
            }
            if (asyncAction.Exception != null) {
                throw new InvalidProgramException(asyncAction.Exception.Message, asyncAction.Exception);
            }
        }

        public static void Invoke(IntPtr hWnd, Action action) {
            EndInvoke(BeginInvoke(hWnd, action));
        }

        private static IntPtr OnGetMessage(int code, UIntPtr wparam, IntPtr lparam) {
            if (code == HC_ACTION) {
                unsafe {
                    MSG* msg = (MSG*)lparam;

                    if (msg->message == messageCode) {
                        while (true) {
                            AsyncAction action;
                            lock (actions) {
                                if (actions.Count == 0) {
                                    break;
                                }
                                action = actions.Dequeue();
                            }
                            action.Invoke();
                        }
                        return IntPtr.Zero;
                    }
                }
            }
            return CallNextHookEx(hHook, code, wparam, lparam);
        }

        public static void Uninitialize() {
            if (hHook != IntPtr.Zero) {
                UnhookWindowsHookEx(hHook);
                hHook = IntPtr.Zero;
            }
            onGetMessage = null;
        }

        private class AsyncAction : IAsyncResult {
            private readonly Action action;
            private readonly ManualResetEvent completedEvent = new ManualResetEvent(false);
            private bool isCompleted;

            public AsyncAction(Action action) {
                this.action = action;
            }

            public Exception Exception {
                get;
                private set;
            }


            public void Invoke() {
                try {
                    Debug.WriteLine(string.Format("HWndDispatcher: invoking {0}.{1}.", this.action.Method.DeclaringType.FullName, this.action.Method.Name));
                    this.action();
                } catch (Exception e) {
                    this.Exception = e;
                } finally {
                    this.isCompleted = true;
                    this.completedEvent.Set();
                }
            }

            public bool IsCompleted {
                get {
                    return this.isCompleted;
                }
            }

            public WaitHandle AsyncWaitHandle {
                get {
                    return this.completedEvent;
                }
            }

            public object AsyncState {
                get {
                    return null;
                }
            }

            public bool CompletedSynchronously {
                get {
                    return false;
                }
            }
        }
    }
}