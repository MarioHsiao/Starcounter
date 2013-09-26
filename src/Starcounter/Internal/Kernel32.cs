// ***********************************************************************
// <copyright file="Kernel32.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    /// <summary>
    /// Class Kernel32
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class Kernel32
    {

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        /// <summary>
        /// </summary>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public unsafe static extern int GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);

        /// <summary>
        /// Exits the process.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void ExitProcess(uint exitCode);

        /// <summary>
        /// Gets the current process.
        /// </summary>
        /// <returns>IntPtr.</returns>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Gets the proc address.
        /// </summary>
        /// <param name="hModule">The h module.</param>
        /// <param name="procName">Name of the proc.</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        public unsafe static extern void *GetProcAddress(void* hModule, string procName);

        /// <summary>
        /// Loads the library A.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet=CharSet.Ansi)]
        public unsafe static extern void *LoadLibraryA(string fileName);

        /// <summary>
        /// Moves the byte memory.
        /// </summary>
        /// <param name="Destination">The destination.</param>
        /// <param name="Source">The source.</param>
        /// <param name="LengthBytes">The length bytes.</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "RtlMoveMemory")]
        internal unsafe static extern void MoveByteMemory(Byte* Destination, Byte* Source, Int32 LengthBytes);

        /// <summary>
        /// Sets the process priority boost.
        /// </summary>
        /// <param name="hProcess">The h process.</param>
        /// <param name="DisablePriorityBoost">The disable priority boost.</param>
        /// <returns>System.Int32.</returns>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern int SetProcessPriorityBoost(IntPtr hProcess, int DisablePriorityBoost);

        /// <summary>
        /// The ME m_ COMMIT
        /// </summary>
        public const uint MEM_COMMIT = 0x00001000;

        /// <summary>
        /// The PAG e_ READWRITE
        /// </summary>
        public const uint PAGE_READWRITE = 0x04;

        /// <summary>
        /// Virtuals the alloc.
        /// </summary>
        /// <param name="lpAddress">The lp address.</param>
        /// <param name="dwSize">Size of the dw.</param>
        /// <param name="flAllocationType">Type of the fl allocation.</param>
        /// <param name="flProtect">The fl protect.</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public unsafe static extern void *VirtualAlloc(void *lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);
    }
}
