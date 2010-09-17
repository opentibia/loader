using System;
using System.Runtime.InteropServices;

namespace otloader
{
	enum PatchResult
	{
		Dummy,
		CouldNotFindClient,
		CouldNotFindRSA,
		CouldNotPatchRSA,
		CouldNotFindServer,
		CouldNotPatchServer,
		CouldNotPatchServerList,
		CouldNotPatchPort,
		AlreadyPatched,
		AlreadyPatchedNotOwned,
		Success
	};

	class Utils
	{
		private static UInt32 hintAddress = 0;

		private const string tibiaWindowName = "Tibia";
		private const string tibiaClassName = "TibiaClient";
		
#if WIN32
		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern Int32 ReadProcessMemory
		(
			[In] IntPtr hProcess,
			[In] IntPtr lpBaseAddress,
			[Out] byte[] lpBuffer,
			[In] UInt32 nSize,
			[In, Out] ref UInt32 lpNumberOfBytesRead
		);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern Int32 WriteProcessMemory(
			[In] IntPtr hProcess,
			[In] IntPtr lpBaseAddress,
			[In] byte[] lpBuffer,
			[In] UInt32 nSize,
			[In, Out] ref UInt32 lpNumberOfBytesWritten
		);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern IntPtr FindWindow(
			string lpClassName,
			string lpWindowName);
		
		[DllImport("kernel32.dll")]
		static extern bool VirtualProtectEx(
			IntPtr hProcess,
			IntPtr lpAddress,
			UIntPtr dwSize,
			UInt32 flNewProtect,
			out UInt32 lpflOldProtect);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(
			UInt32 dwDesiredAccess,
			Int32 bInheritHandle,
			UInt32 dwProcessId
		);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(
			IntPtr hWnd,
			out UInt32 lpdwProcessId
		);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern Int32 CloseHandle(
			IntPtr hObject
		);

		private static bool GetMemoryRange(
			IntPtr hProcess,
			UInt32 nIndex,
			ref UInt32 nStartAddress,
			ref UInt32 nEndAddress
		)
		{
			if (nIndex > 0)
			{
				return false;
			}

			nStartAddress = 0x00400000;
			nEndAddress   = 0x7FFFFFFF - 0x00400000;
			return true;
		}
#else
		const string libpath = "./libptrace.so";

		[DllImport(libpath, SetLastError = true)]
		private static extern Int32 ReadProcessMemory
		(
			[In] IntPtr hProcess,
			[In] IntPtr lpBaseAddress,
			[Out] byte[] lpBuffer,
			[In] UInt32 nSize,
			[In, Out] ref UInt32 lpNumberOfBytesRead
		);

		[DllImport(libpath, SetLastError = true)]
		private static extern Int32 WriteProcessMemory(
			[In] IntPtr hProcess,
			[In] IntPtr lpBaseAddress,
			[In] byte[] lpBuffer,
			[In] UInt32 nSize,
			[In, Out] ref UInt32 lpNumberOfBytesWritten
		);

		[DllImport(libpath, EntryPoint="PidOf")]
		private static extern Int32 PidOf(
			string lpWindowName
		);
		
		[DllImport(libpath, EntryPoint="GetMemoryRange")]
		private static extern bool GetMemoryRange(
			[In] IntPtr hProcess,
			[In] UInt32 nIndex,
			[In, Out] ref UInt32 nStartAddress,
			[In, Out] ref UInt32 nEndAddress
		);
#endif
		private static bool ByteArrayCompare(byte[] a1, UInt32 startOffset, byte[] a2, UInt32 compareLength)
		{
			if (a1.Length < compareLength || a2.Length < compareLength)
				return false;

			for (Int32 i = 0; i < compareLength; i++)
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
		
		private static bool SearchBuffer(byte[] buffer, UInt32 bufferCount, byte[] bytePattern, ref UInt32 index)
		{
			for(index = 0; index < bufferCount; ++index)
			{
				if (ByteArrayCompare(buffer, index, bytePattern, (UInt32)bytePattern.Length))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private static bool SearchBytes(
			IntPtr processHandle,
			byte[] bytePattern,
			ref UInt32 hintAddress,
			out IntPtr outAddress)
		{
			byte[] buffer = new byte[0x2000 * 32];
			UInt32 bytesRead = 0;

			outAddress = IntPtr.Zero;
			if(hintAddress != 0)
			{
				//try a quick search with the help of the hintAddress
				ReadProcessMemory(processHandle, (IntPtr)hintAddress, buffer, (UInt32)buffer.Length, ref bytesRead);
				if(bytesRead >= bytePattern.Length)
				{
					UInt32 bufferIndex = 0;
					if(SearchBuffer(buffer, bytesRead, bytePattern, ref bufferIndex))
					{
						outAddress = (IntPtr)(hintAddress + bufferIndex);
						return true;
					}
				}
				
				hintAddress = 0;
			}

			//Full memory scan
			UInt32 addressIndex = 0;
			UInt32 startAddress = 0;
			UInt32 endAddress = 0;

			while(GetMemoryRange(processHandle, addressIndex, ref startAddress, ref endAddress))
			{
				UInt32 readPos = startAddress;
				do
				{
					ReadProcessMemory(processHandle, (IntPtr)readPos, buffer, (UInt32)buffer.Length, ref bytesRead);
					if(bytesRead < bytePattern.Length)
					{
						// Not enough data to search in
						break;
					}
					
					UInt32 bufferIndex = 0;
					if(SearchBuffer(buffer, bytesRead, bytePattern, ref bufferIndex))
					{
						outAddress = (IntPtr)(readPos + bufferIndex);
						return true;
					}
	
					readPos = readPos + ((UInt32)buffer.Length) - ((UInt32)bytePattern.Length);
				}
				while (readPos < endAddress);
				++addressIndex;
			}

			return false;
		}

		private static IntPtr GetProcessHandle()
		{
#if WIN32
			UInt32 processId = GetProcessId();
			if (processId == 0)
			{
				return IntPtr.Zero;
			}

			return OpenProcess(0x1F0FFF, 1, processId);
#else
			return (IntPtr)GetProcessId();
#endif
		}

		private static bool PatchMemory(IntPtr processHandle, IntPtr address, byte[] patchBytes, bool removeProtect)
		{
#if WIN32
			UInt32 lpfOldProtect = 0;
			if (removeProtect)
			{
				const uint PAGE_EXECUTE_READWRITE = 0x40;
				VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, PAGE_EXECUTE_READWRITE, out lpfOldProtect);
			}
#endif
			UInt32 bytesWritten = 0;
			Int32 result = WriteProcessMemory(processHandle, address, patchBytes, (UInt32)patchBytes.Length, ref bytesWritten);
#if WIN32
			if (removeProtect)
			{
				UInt32 lpfDummy;
				VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, lpfOldProtect, out lpfDummy);
			}
#endif

			return result != 0;
		}

		public static UInt32 GetProcessId()
		{
#if WIN32
			IntPtr windowHandle = FindWindow(tibiaClassName, null);
			if (windowHandle == IntPtr.Zero)
			{
				return 0;
			}

			UInt32 processId;
			GetWindowThreadProcessId(windowHandle, out processId);
			return processId;
#else
			return PidOf(tibiaWindowName);
#endif
		}

		public static PatchResult PatchClientRSAKey(string oldRSAKey, string newRSAKey)
		{
			IntPtr processHandle = GetProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return PatchResult.CouldNotFindClient;
			}

			IntPtr address;
			if (!SearchBytes(processHandle, ToByteArray(oldRSAKey), ref hintAddress, out address))
			{
				if (!SearchBytes(processHandle, ToByteArray(newRSAKey), ref hintAddress, out address))
				{
					#if WIN32
					CloseHandle(processHandle);
					#endif
					return PatchResult.CouldNotFindRSA;
				}

				#if WIN32
				CloseHandle(processHandle);
				#endif
				return PatchResult.AlreadyPatched;
			}

			byte[] byteRSAKey = ToByteArray(newRSAKey);
			if (!PatchMemory(processHandle, address, byteRSAKey, true))
			{
				#if WIN32
				CloseHandle(processHandle);
				#endif
				return PatchResult.CouldNotPatchRSA;
			}

			#if WIN32
			CloseHandle(processHandle);
			#endif
			return PatchResult.Success;
		}

		public static PatchResult PatchClientServer(string oldServer, string newServer, UInt16 port)
		{
			IntPtr processHandle = GetProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return PatchResult.CouldNotFindClient;
			}

			byte[] byteOldServer = ToByteArray(oldServer);
			ResizeByteArray(ref byteOldServer, 100);

			IntPtr address;
			if (!SearchBytes(processHandle, byteOldServer, ref hintAddress, out address))
			{
				#if WIN32
				CloseHandle(processHandle);
				#endif
				return PatchResult.CouldNotFindServer;
			}

			byte[] byteNewServer = ToByteArray(newServer);
			if (byteNewServer.Length < byteOldServer.Length)
			{
				ResizeByteArray(ref byteNewServer, byteOldServer.Length);
			}

			if (!PatchMemory(processHandle, address, byteNewServer, false))
			{
				return PatchResult.CouldNotPatchServer;
			}

			byte[] bytePort = BitConverter.GetBytes(port);
			if (!PatchMemory(processHandle, (IntPtr)((Int32)address + byteNewServer.Length), bytePort, false))
			{
				return PatchResult.CouldNotPatchPort;
			}
			
			if(hintAddress == 0)
			{
				hintAddress = ((UInt32)address) - 1000;
			}

			#if WIN32
			CloseHandle(processHandle);
			#endif
			return PatchResult.Success;
		}
	}
}
