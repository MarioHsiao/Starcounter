using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Starcounter.Management.Win32
{
public static class Win32Kernel
{
    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetProcessHeap();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hHandle);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    internal static extern void MoveMemory(IntPtr Destination, IntPtr Source, int Length);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    public unsafe static extern void MoveByteMemory(Byte* Destination, Byte* Source, Int32 LengthBytes);

    [Flags]
    internal enum FormatMessageFlags
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000
    }

    [DllImport("kernel32.dll", EntryPoint = "FormatMessageW")]
    internal static extern int FormatMessage(
        FormatMessageFlags dwFlags,
        IntPtr lpSource,
        int dwMessageId,
        int dwLanguageId,
        IntPtr lpBuffer,
        int nSize,
        IntPtr Arguments
    );

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr SecurityDescriptor;
        public int InheritHandle;
    }

    [Flags]
    public enum FileAccess : uint
    {
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000
    }

    [Flags]
    public enum FileShare : uint
    {
        None = 0x00000000,
        Read = 0x00000001,
        Write = 0x00000002,
        Delete = 0x00000004
    }

    [Flags]
    public enum FileAttributes : uint
    {
        Readonly = 0x00000001,
        Hidden = 0x00000002,
        System = 0x00000004,
        Directory = 0x00000010,
        Archive = 0x00000020,
        Device = 0x00000040,
        Normal = 0x00000080,
        Temporary = 0x00000100,
        SparseFile = 0x00000200,
        ReparsePoint = 0x00000400,
        Compressed = 0x00000800,
        Offline = 0x00001000,
        NotContentIndexed = 0x00002000,
        Encrypted = 0x00004000,
        Write_Through = 0x80000000,
        Overlapped = 0x40000000,
        NoBuffering = 0x20000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        DeleteOnClose = 0x04000000,
        BackupSemantics = 0x02000000,
        PosixSemantics = 0x01000000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00100000,
        FirstPipeInstance = 0x00080000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern SafeFileHandle CreateFileTransacted(
        string fileName,
        FileAccess fileAccess,
        FileShare fileShare,
        SECURITY_ATTRIBUTES securityAttributes,
        System.IO.FileMode creationDisposition,
        FileAttributes flags,
        IntPtr template,
        IntPtr transaction,
        IntPtr miniVersion,
        IntPtr extendedOpenInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool DeleteFileTransacted(string file,
                                                   IntPtr transaction);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool MoveFileTransacted(
        string existingFile,
        string newFileName,
        IntPtr progressRoutine,
        IntPtr progressData,
        uint flags,
        IntPtr transaction);

    public static Win32Exception CreateWin32Exception(int error, string message)
    {
        return new Win32Exception(error, message + " " + GetMessage((Win32Error) error));
    }

    public static Win32Exception CreateWin32Exception(string message)
    {
        int error = Marshal.GetLastWin32Error();
        return new Win32Exception(error, message.TrimEnd('.', ':', ' ') + ": " + GetMessage((Win32Error) error));
    }


    public static string GetMessage(this Win32Error error)
    {
        IntPtr buffer = IntPtr.Zero;
        unsafe
        {
            int size =
            FormatMessage(
                FormatMessageFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero,
                (int) error, 0, (IntPtr)( & buffer), 0, IntPtr.Zero);

            if (size > 0)
            {
                string message = Marshal.PtrToStringUni(buffer);
                LocalFree(buffer);
                return message;
            }
            else
                { return string.Format("Error 0x{0:x}", (int) error); }
        }
    }
}
}