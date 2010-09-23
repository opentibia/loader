using System;
using System.Diagnostics;
using System.IO;
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
		private const string tibiaWindowName = "Tibia";
		private const string tibiaClassName = "TibiaClient";
		private const string tibiaRSAClientText = "Symmetric encryption";
		private static MemoryStream memoryStream = new MemoryStream(1024 * 1024 * 4);
		private static IntPtr memoryStreamOwnerHandle = IntPtr.Zero;

#if WIN32
		private const UInt32 tibiaMutexHandle = 0xF8;
		private const UInt32 baseAddress = 0x00400000;

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenMutex(
			UInt32 dwDesiredAccess,
			bool bInheritHandle,
			string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DuplicateHandle(
			IntPtr hSourceProcessHandle,
			IntPtr hSourceHandle,
			IntPtr hTargetProcessHandle,
			out IntPtr lpTargetHandle,
			UInt32 dwDesiredAccess,
			[MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			UInt32 dwOptions);

		[DllImport("kernel32.dll", SetLastError = true)]
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

		[DllImport("user32.DLL", SetLastError = true)]
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

			nStartAddress = baseAddress;
			nEndAddress   = 0x7FFFFFFF - baseAddress;
			return true;
		}
#else
		private const string libpath = "./libptrace.so";
		private const UInt32 baseAddress = 0x08048000;
		private const string clientAtomName = "TIBIARUNNING";

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

		[DllImport(libpath, SetLastError = true)]
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

		[DllImport(libpath, SetLastError = true)]
		private static extern bool ClearAtomOwner(
			string lpAtomName
		);

		private static Int32 CloseHandle(IntPtr hObject)
		{
			//do nothing
			return 1;
		}

#endif
		public static UInt32 GetClientProcessId()
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
			return (UInt32)PidOf(tibiaWindowName);
#endif
		}

		public static bool PatchMultiClient()
		{
#if WIN32
			IntPtr clientHandle = GetClientProcessHandle();
			if (clientHandle == IntPtr.Zero)
			{
				return false;
			}

			IntPtr processHandle = GetOwnProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return false;
			}

			IntPtr dupHandle;
			const UInt32 DUPLICATE_CLOSE_SOURCE = (0x00000001);

			bool result = false;
			if (DuplicateHandle(clientHandle, (IntPtr)tibiaMutexHandle, processHandle, out dupHandle, 0, false, DUPLICATE_CLOSE_SOURCE))
			{
				CloseHandle(dupHandle);
				result = true;
			}

			CloseHandle(clientHandle);
			CloseHandle(processHandle);

			return result;
#else
			return ClearAtomOwner(clientAtomName);
#endif
		}

		public static bool isClientUsingRSA()
		{
			IntPtr processHandle = GetClientProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return false;
			}

			IntPtr address;
			return SearchMemory(processHandle, ToByteArray(tibiaRSAClientText), out address);
		}

		public static PatchResult PatchClientRSAKey(string oldRSAKey, string newRSAKey)
		{
			IntPtr processHandle = GetClientProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return PatchResult.CouldNotFindClient;
			}

			IntPtr address;
			if (!SearchMemory(processHandle, ToByteArray(oldRSAKey), out address))
			{
				if (!SearchMemory(processHandle, ToByteArray(newRSAKey), out address))
				{
					CloseHandle(processHandle);
					return PatchResult.CouldNotFindRSA;
				}

				CloseHandle(processHandle);
				return PatchResult.AlreadyPatched;
			}

			byte[] byteRSAKey = ToByteArray(newRSAKey);
			if (!WriteMemory(processHandle, address, byteRSAKey))
			{
				CloseHandle(processHandle);
				return PatchResult.CouldNotPatchRSA;
			}

			CloseHandle(processHandle);
			return PatchResult.Success;
		}

		public static PatchResult PatchClientServer(string oldServer, string newServer, UInt16 port)
		{
			IntPtr processHandle = GetClientProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return PatchResult.CouldNotFindClient;
			}

			byte[] byteOldServer = ToByteArray(oldServer);
			ResizeByteArray(ref byteOldServer, 100);

			IntPtr address;
			if (!SearchMemory(processHandle, byteOldServer, out address))
			{
				CloseHandle(processHandle);
				return PatchResult.CouldNotFindServer;
			}

			if (!PatchClientServer(processHandle, address, newServer, port))
			{
				CloseHandle(processHandle);
				return PatchResult.CouldNotPatchServer;
			}

			CloseHandle(processHandle);
			return PatchResult.Success;
		}

		public static PatchResult PatchClientServer(string oldServer, string newServer, UInt16 port, bool continousSearch)
		{
			bool foundServer = false;
			while (true)
			{
				if (PatchClientServer(oldServer, newServer, port) == PatchResult.Success)
				{
					foundServer = true;
					if (continousSearch)
					{
						continue;
					}
				}

				break;
			}

			return (foundServer ? PatchResult.Success : PatchResult.CouldNotPatchServerList);
		}

		//private functions
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

		private static bool SearchMemory(IntPtr processHandle, byte[] bytePattern, out IntPtr outAddress)
		{
			byte[] buffer = new byte[0x2000 * 32];
			return SearchMemory(processHandle, bytePattern, out outAddress, ref buffer);
		}

		private static bool SearchMemory(IntPtr processHandle, byte[] bytePattern, out IntPtr outAddress, ref byte[] buffer)
		{
			if (memoryStreamOwnerHandle != processHandle)
			{
				memoryStream.Dispose();
				memoryStream = new MemoryStream(1024 * 1024 * 4);
				memoryStreamOwnerHandle = processHandle;
			}

			outAddress = IntPtr.Zero;
			UInt32 addressIndex = 0;
			UInt32 startAddress = 0;
			UInt32 endAddress = 0;

			//Complete memory scan
			while(GetMemoryRange(processHandle, addressIndex, ref startAddress, ref endAddress))
			{
				if((endAddress - startAddress) == 0)
				{
					//memory range is valid, but not what we are looking for
					++addressIndex;
					continue;
				}

				bool isMemStream;
				UInt32 readPos = startAddress;
				do
				{
					UInt32 bytesRead = 0;
					ReadMemory(processHandle, (IntPtr)readPos, ref buffer, ref bytesRead, out isMemStream);

					if (bytesRead < bytePattern.Length)
					{
						//Not enough data to search in
						break;
					}

					if (!isMemStream)
					{
						memoryStream.Position = readPos - baseAddress;
						memoryStream.Write(buffer, 0, (Int32)bytesRead);
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

		private static IntPtr GetClientProcessHandle()
		{
#if WIN32
			UInt32 processId = GetClientProcessId();
			if (processId == 0)
			{
				return IntPtr.Zero;
			}

			return OpenProcess(0x1F0FFF, 1, processId);
#else
			return (IntPtr)GetClientProcessId();
#endif
		}

		private static IntPtr GetOwnProcessHandle()
		{
			UInt32 processId = (UInt32)Process.GetCurrentProcess().Id;
			if (processId == 0)
			{
				return IntPtr.Zero;
			}
#if WIN32
			return OpenProcess(0x1F0FFF, 1, processId);
#else
			return (IntPtr)processId;
#endif
		}

		private static bool WriteMemory(IntPtr processHandle, IntPtr address, byte[] patchBytes)
		{
#if WIN32
			UInt32 lpfOldProtect = 0;
			const uint PAGE_EXECUTE_READWRITE = 0x40;
			VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, PAGE_EXECUTE_READWRITE, out lpfOldProtect);
#endif
			memoryStream.Position = ((UInt32)address) - baseAddress;
			memoryStream.Write(patchBytes, 0, (Int32)patchBytes.Length);

			UInt32 bytesWritten = 0;
			Int32 result = WriteProcessMemory(processHandle, address, patchBytes, (UInt32)patchBytes.Length, ref bytesWritten);
#if WIN32
			UInt32 lpfDummy;
			VirtualProtectEx(processHandle, address, (UIntPtr)patchBytes.Length, lpfOldProtect, out lpfDummy);
#endif
			return (result != 0);
		}

		private static bool ReadMemory(IntPtr processHandle, IntPtr readPos, ref byte[] buffer, ref UInt32 bytesRead, out bool isMemStream)
		{
			isMemStream = false;
			UInt32 relPos = ((UInt32)readPos) - baseAddress;
			if (memoryStream.Length > relPos + buffer.Length)
			{
				isMemStream = true;
				memoryStream.Seek(relPos, SeekOrigin.Begin);
				bytesRead = (UInt32)memoryStream.Read(buffer, 0, (Int32)buffer.Length);
				if (bytesRead >= buffer.Length)
				{
					return true;
				}
			}

			ReadProcessMemory(processHandle, readPos, buffer, (UInt32)buffer.Length, ref bytesRead);
			return (bytesRead > 0);
		}

		private static bool PatchClientServer(IntPtr processHandle, IntPtr address, string newServer, UInt16 port)
		{
			byte[] byteNewServer = ToByteArray(newServer);
			if (byteNewServer.Length < 100)
			{
				ResizeByteArray(ref byteNewServer, 100);
			}

			if (!WriteMemory(processHandle, address, byteNewServer))
			{
				return false;
			}

			byte[] bytePort = BitConverter.GetBytes(port);
			if (!WriteMemory(processHandle, (IntPtr)((Int32)address + byteNewServer.Length), bytePort))
			{
				return false;
			}

			return true;
		}
	}
}
