using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Birdie.Data;
using Birdie.Process;
using System.IO;
using Birdie.Utility;
using Birdie.Watcher;

namespace Birdie.Interop
{
    public static class ProcessReader
    {
        #region PInvoke
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_QUERY_INFORMATION = 0x0400;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern uint GetProcessImageFileName(IntPtr hProcess, [Out] StringBuilder lpImageFileName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);
        #endregion

        #region Methods
        internal static ProcessData OpenProcess(int processId)
        {
            IntPtr handle = OpenProcess(PROCESS_WM_READ | PROCESS_QUERY_INFORMATION, false, processId);

            if (handle.ToInt64() == 0)
            {
                Debug.WriteLine(string.Format(@"BirdieCore: OpenProcess for id {0} failed with error: {1}!", processId, Win32Error.GetLastWin32Error()));
                return null;
            }

            StringBuilder sbuilder = new StringBuilder(1024);
            GetProcessImageFileName(handle, sbuilder, (int)sbuilder.Capacity);

            return new ProcessData { ProcessHandle = handle, ProcessName = Path.GetFileName(sbuilder.ToString()), ProcessId = (ulong)processId };
        }

        public static byte[] ReadProcessMemory(WatchMemoryObject watchMemoryObject)
        {
            byte[] buffer = new byte[watchMemoryObject.MaxSize];
            ProcessData processData = watchMemoryObject.ProcessData;

            bool successful = ReadProcessMemory(processData.ProcessHandle, (UIntPtr)watchMemoryObject.BaseAddress, buffer, buffer.Length, IntPtr.Zero);

            if (!successful)
            {
                watchMemoryObject.LastError = string.Format("Could not read memory: {0}", Win32Error.GetLastWin32Error());
                return null;
            }

            return buffer;
        }
        #endregion
    }
}
