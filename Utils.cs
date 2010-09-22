using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

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

#if WIN32
		private const UInt32 tibiaMutexHandle = 0xF8;

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

			nStartAddress = 0x00400000;
			nEndAddress   = 0x7FFFFFFF - 0x00400000;
			return true;
		}
#else
		private const string clientAtomName = "TIBIARUNNING";

		private const string libpath = "./libptrace.so";

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

		Int32 CloseHandle(IntPtr hObject)
		{
			//do nothing
		}

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

		private static bool SearchMemory(IntPtr processHandle, byte[] bytePattern, out IntPtr outAddress)
		{
			byte[] buffer = new byte[0x2000 * 32];
			return SearchMemory(processHandle, bytePattern, out outAddress, ref buffer);
		}

		private static bool SearchMemory(IntPtr processHandle, byte[] bytePattern, out IntPtr outAddress, ref byte[] buffer)
		{
			outAddress = IntPtr.Zero;
			UInt32 addressIndex = 0;
			UInt32 startAddress = 0;
			UInt32 endAddress = 0;

			while(GetMemoryRange(processHandle, addressIndex, ref startAddress, ref endAddress))
			{
				UInt32 readPos = startAddress;
				do
				{
					UInt32 bytesRead = 0;
					if (!ReadMemory(processHandle, (IntPtr)readPos, ref buffer, ref bytesRead))
					{
						break;
					}

					if (bytesRead < bytePattern.Length)
					{
						//Not enough data to search in
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
			if(processId == 0)
			{
				return IntPtr.Zero;
			}
#if WIN32
			return OpenProcess(0x1F0FFF, 1, processId);
#else
			return (IntPtr)processId;
#endif
		}

		private static bool WriteMemory(IntPtr processHandle, IntPtr address, byte[] patchBytes, bool removeProtect)
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
			return (result != 0);
		}

		private static bool ReadMemory(IntPtr processHandle, IntPtr readPos, ref byte[] buffer, ref UInt32 bytesRead)
		{
			ReadProcessMemory(processHandle, readPos, buffer, (UInt32)buffer.Length, ref bytesRead);
			return bytesRead > 0;
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
				CloseHandle((IntPtr)dupHandle);
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
			if (!WriteMemory(processHandle, address, byteRSAKey, true))
			{
				CloseHandle(processHandle);
				return PatchResult.CouldNotPatchRSA;
			}

			CloseHandle(processHandle);
			return PatchResult.Success;
		}

		private static bool PatchClientServer(IntPtr processHandle, IntPtr address, string newServer, UInt16 port)
		{
			byte[] byteNewServer = ToByteArray(newServer);
			if (byteNewServer.Length < 100)
			{
				ResizeByteArray(ref byteNewServer, 100);
			}

			if (!WriteMemory(processHandle, address, byteNewServer, false))
			{
				return false;
			}

			byte[] bytePort = BitConverter.GetBytes(port);
			if (!WriteMemory(processHandle, (IntPtr)((Int32)address + byteNewServer.Length), bytePort, false))
			{
				return false;
			}

			return true;
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

		public static PatchResult PatchClientServerList(List<string> clientServerList, string newServer, UInt16 port)
		{
			IntPtr processHandle = GetClientProcessHandle();
			if (processHandle == IntPtr.Zero)
			{
				return PatchResult.CouldNotFindClient;
			}

			byte[] clientServerBuffer = new byte[100];
			IntPtr firstClientServerAddress = IntPtr.Zero;

			byte[] buffer = new byte[0x2000 * 32];
			foreach (string clientServer in clientServerList)
			{
				byte[] byteOldServer = ToByteArray(clientServer);
				ResizeByteArray(ref byteOldServer, 100);

				IntPtr address;
				if (SearchMemory(processHandle, byteOldServer, out address, ref buffer))
				{
					//We found a server in the memory, now search backwards
					//until we find the first server.
					firstClientServerAddress = address;

					bool foundServer = false;
					do
					{
						//The client structure for a server looks something like (112 bytes)
						/*
						struct ClientServer
						 * {
						 *		byte[100] server;
						 *		UInt32 port;
						 *		UInt32 unknown;  //Usually 1
						 *		UInt32 unknown;
						 * };
						*/
						UInt32 bytesRead = 0;
						if (!ReadMemory(processHandle, (IntPtr)((UInt32)firstClientServerAddress - 112), ref clientServerBuffer, ref bytesRead))
						{
							break;
						}

						if (bytesRead < clientServerBuffer.Length)
						{
							//Not enough data to search in
							continue;
						}

						foundServer = false;
						foreach (string prevClientServer in clientServerList)
						{
							byte[] bytePrevClientServer = ToByteArray(prevClientServer);
							ResizeByteArray(ref bytePrevClientServer, 100);

							if (ByteArrayCompare(bytePrevClientServer, 0, clientServerBuffer, (UInt32)clientServerBuffer.Length))
							{
								firstClientServerAddress = (IntPtr)((UInt32)firstClientServerAddress - 112);
								foundServer = true;
								break;
							}
						}
					} while (foundServer);
					break;
				}
			}

			if (firstClientServerAddress != IntPtr.Zero)
			{
				IntPtr writeAddress = firstClientServerAddress;
				bool foundServer = false;
				bool foundMatch = false;
				do{
					UInt32 bytesRead = 0;
					if (!ReadMemory(processHandle, writeAddress, ref clientServerBuffer, ref bytesRead))
					{
						CloseHandle(processHandle);
						return (foundServer ? PatchResult.Success : PatchResult.CouldNotPatchServerList);
					}

					foundMatch = false;
					foreach (string clientServer in clientServerList)
					{
						byte[] byteClientServer = ToByteArray(clientServer);
						ResizeByteArray(ref byteClientServer, 100);

						if (ByteArrayCompare(byteClientServer, 0, clientServerBuffer, (UInt32)clientServerBuffer.Length))
						{
							if(PatchClientServer(processHandle, writeAddress, newServer, port)){
								writeAddress = (IntPtr)((UInt32)writeAddress + 112);
								foundServer = true;
								foundMatch = true;
								break;
							}
						}
					}
				} while (foundMatch);

				return (foundServer ? PatchResult.Success : PatchResult.CouldNotPatchServerList);
			}

			CloseHandle(processHandle);
			return PatchResult.CouldNotPatchServerList;
		}
	}
}
