using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace otloader
{
    class Utils
    {
        [DllImport("USER32.DLL", SetLastError = true)]
        private static extern IntPtr FindWindow(
            string lpClassName,
            string lpWindowName);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory
        (
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            [In] UInt32 nSize,
            [In, Out] ref UInt32 lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [In] byte[] lpBuffer,
            [In] UInt32 nSize,
            [In, Out] ref UInt32 lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
           UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            UInt32 dwProcessId
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint lpdwProcessId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Int32 CloseHandle(
            IntPtr hObject
        );

        private static bool ByteArrayCompare(byte[] a1, UInt32 startOffset, byte[] a2, UInt32 compareLength)
        {
            if (a1.Length < compareLength || a2.Length < compareLength)
                return false;

            for (int i = 0; i < compareLength; i++)
            {
                if (startOffset + i >= a1.Length)
                    return false;

                if (a1[startOffset + i] != a2[i])
                    return false;
            }

            return true;
        }

        private static void ResizeByteArray(ref byte[] array, Int32 newSize)
        {
            Array.Resize<byte>(ref array, newSize);

        }
        private static byte[] ToByteArray(string s)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(s);
        }

        private static bool SearchBytes(IntPtr processHandle, byte[] bytePattern, out IntPtr outAddress)
        {
	        outAddress = IntPtr.Zero;

	        byte[] buffer = new byte[0x1000 * 32];
	        UInt32 bytesRead = 0;

            Int64 startAddress = 0x00400000;
            Int64 readPos = startAddress;
            do
            {
                ReadProcessMemory(processHandle, (IntPtr)readPos, buffer, (UInt32)buffer.Length, ref bytesRead);

                if (bytesRead != buffer.Length)
                {
                    return false;
                }

                for (UInt32 i = 0; i < buffer.Length; i++)
                {
                    if (ByteArrayCompare(buffer, i, bytePattern, (UInt32)bytePattern.Length))
                    {
                        outAddress = (IntPtr)(readPos + i);
                        return true;
                    }
                }

                readPos = readPos + buffer.Length - (bytePattern.Length);

            } while (readPos < 0x7FFFFFFF);

	        return false;
        }

        const string tibiaClassName = "TibiaClient";

        public static bool IsClientRunning()
        {
            IntPtr windowHandle = FindWindow(tibiaClassName, null);
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            return true;
        }

        private static IntPtr GetProcessHandle()
        {
            IntPtr windowHandle = FindWindow(tibiaClassName, null);
            if (windowHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            UInt32 processId;
            GetWindowThreadProcessId(windowHandle, out processId);
            IntPtr processHandle = OpenProcess(0x1F0FFF, 1, processId);

            return processHandle;
        }

        private static bool PatchMemory(IntPtr processHandle, IntPtr address, byte[] patchBytes, bool removeProtect)
        {
            const uint PAGE_EXECUTE_READWRITE = 0x40;

            uint lpfOldProtect = 0;
            if (removeProtect)
            {
                VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, PAGE_EXECUTE_READWRITE, out lpfOldProtect);
            }

            UInt32 bytesWritten = 0;
            bool result = WriteProcessMemory(processHandle, address, patchBytes, (UInt32)patchBytes.Length, ref bytesWritten);

            if (removeProtect)
            {
                uint lpfDummy;
                VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, lpfOldProtect, out lpfDummy);
            }

            return result;
        }

        public static bool PatchClientRSAKey(string oldRSAKey, string newRSAKey)
        {
            IntPtr processHandle = GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr address;
            if (!SearchBytes(processHandle, ToByteArray(oldRSAKey), out address))
            {
                CloseHandle(processHandle);
                return false;
            }

            byte[] byteRSAKey = ToByteArray(newRSAKey);
            if (!PatchMemory(processHandle, address, byteRSAKey, true))
            {
                return false;
            }

            CloseHandle(processHandle);
            return true;
        }

        public static bool PatchClientServer(string oldServer, string newServer, Int16 port)
        {
            IntPtr processHandle = GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr address;
            byte[] byteOldServer = ToByteArray(oldServer);
            ResizeByteArray(ref byteOldServer, 100);

            if (!SearchBytes(processHandle, byteOldServer, out address))
            {
                CloseHandle(processHandle);
                return false;
            }

            byte[] byteNewServer = ToByteArray(newServer);
            
            if (byteNewServer.Length < byteOldServer.Length)
            {
                ResizeByteArray(ref byteNewServer, byteOldServer.Length);
            }

            if (!PatchMemory(processHandle, address, byteNewServer, false))
            {
                return false;
            }

            byte[] bytePort = BitConverter.GetBytes(port);
            if (!PatchMemory(processHandle, (IntPtr)((int)address + byteNewServer.Length), bytePort, false))
            {
                return false;
            }

            CloseHandle(processHandle);
            return true;
        }
    }
}
