using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PG3DInjector
{
    public class Inject
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;
        
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        public static void Load(string process, string dll)
        {
            int maxRetries = 10;
            int delayMilliseconds = 5000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Console.WriteLine($"Attempt {attempt} to find target process '{process}'...");
                Process targetProcess = GetProcessByName(process);
                if (targetProcess != null)
                {
                    Console.WriteLine($"Target process '{process}' found. Injecting '{dll}'...");
                    InjectDLL(targetProcess, dll);
                    return;
                }
                else
                {
                    if (attempt < maxRetries)
                    {
                        Console.WriteLine($"Target process '{process}' not found. Retrying in {delayMilliseconds / 1000} seconds...");
                        Thread.Sleep(delayMilliseconds);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to find target process '{process}' after {maxRetries} retries.");
                    }
                }
            }
        }

        private static Process GetProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0 ? processes[0] : null;
        }

        private static void InjectDLL(Process targetProcess, string dll)
        {
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                false, targetProcess.Id);
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dll.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE,
                PAGE_READWRITE);
            UIntPtr bytesWritten;
            WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dll), (uint)((dll.Length + 1) * Marshal.SizeOf(typeof(char))),
                out bytesWritten);
            CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        }


    }
}