#include <cstring>
#include <cstdlib>
#include <sys/ptrace.h>
#include <stdio.h>
#include <sys/wait.h>
#include <errno.h>
#include <iostream>
#include <elf.h>
#include <X11/Xlib.h>
#include <X11/Xatom.h>

extern "C" int ReadProcessMemory(
	int pid,
	unsigned int lpBaseAddress,
	unsigned char* lpBuffer,
	unsigned int nSize,
	unsigned int* lpNumberOfBytesRead)
{
	*lpNumberOfBytesRead = 0;
	
	errno = 0;
	if(ptrace(PTRACE_ATTACH, pid, NULL, NULL) < 0){
#if DEBUG
		perror("PTRACE_ATTACH");
#endif
		return 0;
	}
	
	waitpid(pid, NULL, WUNTRACED);

	long peekData;
	
	unsigned char* buffer = lpBuffer;

	unsigned int i = 0;
	int longCount = nSize / sizeof(peekData);
	while(i < longCount)
	{
		errno = 0;
		peekData = ptrace(PTRACE_PEEKDATA, pid, lpBaseAddress + i * sizeof(peekData), NULL);
		
		if(peekData == -1 && errno)
		{
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_PEEKDATA");
#endif
			return 0;
		}
		memcpy(buffer, &peekData, sizeof(peekData));
		(*lpNumberOfBytesRead) += sizeof(peekData);

		++i;
		buffer += sizeof(peekData);
	}
	
	int remainder = nSize % sizeof(peekData);
	if(remainder != 0){
		errno = 0;
		peekData = ptrace(PTRACE_PEEKDATA, pid, lpBaseAddress + i * sizeof(peekData), NULL);
		
		if(peekData == -1 && errno)
		{
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_PEEKDATA");
#endif
			return 0;
		}
		
		memcpy(buffer, &peekData, remainder);
		(*lpNumberOfBytesRead) += remainder;
	}

	errno = 0;
	if(ptrace(PTRACE_DETACH, pid, NULL, NULL) == -1){
#if DEBUG
		perror("PTRACE_DEATCH");
#endif
		return 0;
	}

	return (*lpNumberOfBytesRead);
}

extern "C" int WriteProcessMemory(
	int pid,
	unsigned int lpBaseAddress,
	unsigned char* lpBuffer,
	unsigned int nSize,
	unsigned int* lpNumberOfBytesWritten)
{
	*lpNumberOfBytesWritten = 0;

		errno = 0;
	if(ptrace(PTRACE_ATTACH, pid, NULL, NULL) < 0){
#if DEBUG
		perror("PTRACE_ATTACH");
#endif
		return 0;
	}
	
	waitpid(pid, NULL, WUNTRACED);
	
	long pokeData;
	
	unsigned char* buffer = lpBuffer;
	
	unsigned int i = 0;
	int longCount = nSize / sizeof(pokeData);
	while(i < longCount)
	{
		memcpy(&pokeData, buffer, sizeof(pokeData));
		
		errno = 0;
		if(ptrace(PTRACE_POKEDATA, pid, lpBaseAddress + i * sizeof(pokeData), pokeData) < 0 && errno){
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_POKEDATA");
#endif
			return 0;
		}
		
		++i;
		buffer += sizeof(pokeData);		
		(*lpNumberOfBytesWritten) += sizeof(pokeData);
	}
	
	int remainder = nSize % sizeof(pokeData);
	if(remainder != 0)
	{
		//the remaining data is smaller than pokeData (4 bytes on x32 and 8 bytes on x64 bit systems)
		//so we read in sizeof(word) bytes, clearing the bits that we want to update
		//then OR:ing it with the new data and write it back.
		long peekData = ptrace(PTRACE_PEEKDATA, pid, lpBaseAddress + i * sizeof(peekData), NULL);
		
		//there is probably a better way to do this, but oh well...
		if(sizeof(pokeData) == 4){
			switch(remainder)
			{
				case 1: peekData = peekData & 0xFFFFFF00; break;
				case 2: peekData = peekData & 0xFFFF0000; break;
				case 3: peekData = peekData & 0xFF000000; break;
				default: break;
			}
		}
		else if(sizeof(pokeData) == 8){
			switch(remainder)
			{
				case 1: peekData = peekData & 0xFFFFFFFFFFFFFF00; break;
				case 2: peekData = peekData & 0xFFFFFFFFFFFF0000; break;
				case 3: peekData = peekData & 0xFFFFFFFFFF000000; break;
				case 4: peekData = peekData & 0xFFFFFFFF00000000; break;
				case 5: peekData = peekData & 0xFFFFFF0000000000; break;
				case 6: peekData = peekData & 0xFFFF000000000000; break;
				case 7: peekData = peekData & 0xFF00000000000000; break;
				default: break;
			}
		}
		
		pokeData = 0;
		memcpy(&pokeData, buffer, remainder);
		
		pokeData = peekData | pokeData;
		
		errno = 0;
		if(ptrace(PTRACE_POKEDATA, pid, lpBaseAddress + i * sizeof(pokeData), pokeData) < 0 && errno){
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_POKEDATA");
#endif
			return 0;
		}
		
		(*lpNumberOfBytesWritten) += remainder;
	}
	
	errno = 0;
	if(ptrace(PTRACE_DETACH, pid, NULL, NULL) == -1){
#if DEBUG
		perror("PTRACE_DEATCH");
#endif
		return 0;
	}

	return (*lpNumberOfBytesWritten);
}

extern "C" int PidOf(const char* lpWindowName)
{
	std::string buffer = "pidof -s " + std::string(lpWindowName);
	
	FILE* stream = popen(buffer.c_str(), "r");
	
	if(stream == 0)
		return 0;
		
	char pidBuffer[256] = {0};
	size_t bytesRead = fread(pidBuffer, sizeof(char), sizeof(char) * 255, stream);
	if(bytesRead == 0)
	{
		return 0;
	}
	
	pid_t pid = atoi((char*)&pidBuffer[0]);
	return pid;
}

extern "C" bool GetMemoryRange(int pid, unsigned int index, unsigned int* nStartAddress, unsigned int* nEndAddress)
{
	const int baseAddress = 0x08048000;
	
	Elf32_Ehdr ehdr;
	Elf32_Phdr phdr;

	unsigned int bytesRead = 0;
	ReadProcessMemory(pid, baseAddress, (unsigned char*)&ehdr, sizeof(Elf32_Ehdr), &bytesRead);
	
	/*
	printf("e_shoff: %d\n", ehdr.e_shoff);
	printf("e_shnum: %d\n", ehdr.e_shnum);
	printf("e_phoff: %d\n", ehdr.e_phoff);
	printf("e_phnum: %d\n", ehdr.e_phnum);
	printf("e_shentsize: %d\n", ehdr.e_shentsize);
	*/

	if(index >= ehdr.e_phnum)
	{
		return false;
	}
	
	ReadProcessMemory(
		pid,
		baseAddress + ehdr.e_phoff + sizeof(Elf32_Phdr) * index,
		(unsigned char*)&phdr,
		sizeof(Elf32_Phdr),
		&bytesRead);

	if(phdr.p_type == PT_LOAD){
		//This section contains the information we are interested in
		*nStartAddress = phdr.p_vaddr;
		*nEndAddress = phdr.p_vaddr + phdr.p_memsz;
	}
	else{
		*nStartAddress = 0;
		*nEndAddress = 0;
	}

	return true;
}

extern "C" bool ClearAtomOwner(const char* atomName)
{
	Display* display = XOpenDisplay(NULL);
	Atom atom = XInternAtom(display, atomName, true);
	if((int)atom == 0)
	{
		return false;
	}
	
	XSetSelectionOwner(display, atom, None, CurrentTime);
	XGetSelectionOwner(display, atom);
	
	return true;
}