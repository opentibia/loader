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
	errno = 0;
	if(ptrace(PTRACE_ATTACH, pid, NULL, NULL) < 0){
#if DEBUG
		perror("PTRACE_ATTACH");
#endif
		return 0;
	}
	
	waitpid(pid, NULL, WUNTRACED);

	*lpNumberOfBytesRead = 0;
	long peekData;
	for(unsigned int i = 0; i <= nSize - sizeof(peekData); i += sizeof(peekData)){
		unsigned long addrPtr = lpBaseAddress + i;
		errno = 0;
		peekData = ptrace(PTRACE_PEEKDATA, pid, (void*)addrPtr, NULL);
		
		if(peekData == -1 && errno)
		{
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_PEEKDATA");
#endif
			return 0;
		}
		
		memcpy(lpBuffer + i, &peekData, sizeof(peekData));
		(*lpNumberOfBytesRead) += sizeof(peekData);
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
	errno = 0;
	if(ptrace(PTRACE_ATTACH, pid, NULL, NULL) < 0){
#if DEBUG
		perror("PTRACE_ATTACH");
#endif
		return 0;
	}
	
	waitpid(pid, NULL, WUNTRACED);

	*lpNumberOfBytesWritten = 0;
	
	int longCount = nSize / sizeof(long);
	long pokeData;
	
	unsigned char* buffer = lpBuffer;
	
	unsigned int i = 0;
	while(i < longCount)
	{
		memcpy(&pokeData, buffer, sizeof(long));
		
		errno = 0;
		if(ptrace(PTRACE_POKEDATA, pid, lpBaseAddress + i * 4, pokeData) < 0 && errno){
			ptrace(PTRACE_DETACH, pid, NULL, NULL);
#if DEBUG
			perror("PTRACE_POKEDATA");
#endif
			return 0;
		}
		
		++i;
		buffer += sizeof(long);		
		(*lpNumberOfBytesWritten) += sizeof(long);
	}
	
	int remainder = nSize % sizeof(long);
	if(remainder != 0)
	{
		pokeData = 0;
		memcpy(&pokeData, buffer, remainder);
		
		errno = 0;
		if(ptrace(PTRACE_POKEDATA, pid, lpBaseAddress + i * 4, pokeData) < 0 && errno){
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