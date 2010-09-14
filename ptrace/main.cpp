#include "main.h"

#include <cstring>
#include <cstdlib>
#include <sys/ptrace.h>
#include <string>
#include <stdio.h>
#include <sys/wait.h>
#include <errno.h>
#include <iostream>
#include <iomanip>
#include <elf.h>

extern "C" int ReadProcessMemory(
	int pid,
	unsigned int lpBaseAddress,
	unsigned char* lpBuffer,
	unsigned int nSize,
	unsigned int* lpNumberOfBytesRead)
{
	errno = 0;
    if(ptrace(PTRACE_ATTACH, pid, NULL, NULL) < 0){
	    perror("PTRACE_ATTACH");
        return 0;
    }
	
	waitpid(pid, NULL, WUNTRACED);

	*lpNumberOfBytesRead = 0;
	int peekData;
    for(unsigned int i = 0; i <= nSize - sizeof(peekData); i += sizeof(peekData)){
	    unsigned long addrPtr = lpBaseAddress + i;
	    errno = 0;
       	peekData = ptrace(PTRACE_PEEKDATA, pid, (void*)addrPtr, NULL);
       	
       	if(peekData == -1 && errno)
       	{
	       	ptrace(PTRACE_DETACH, pid, NULL, NULL);
	       	perror("PTRACE_PEEKDATA");
	       	return 0;
       	}
       	
        memcpy(lpBuffer + i, &peekData, sizeof(peekData));
		(*lpNumberOfBytesRead) += sizeof(peekData);
    }

	errno = 0;
	if(ptrace(PTRACE_DETACH, pid, NULL, NULL) == -1){
		perror("PTRACE_DEATCH");
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
	    perror("PTRACE_ATTACH");
        return 0;
    }
	
	waitpid(pid, NULL, WUNTRACED);

	*lpNumberOfBytesWritten = 0;
	unsigned char pokeByte;
    for(unsigned int i = 0; i < nSize; i += sizeof(pokeByte)){
        memcpy(&pokeByte, &lpBuffer[i], 1);
        
	    unsigned long addrPtr = lpBaseAddress + i;
	    errno = 0;
        if(ptrace(PTRACE_POKEDATA, pid, (void*)addrPtr, pokeByte) < 0 && errno){
	       	ptrace(PTRACE_DETACH, pid, NULL, NULL);
	       	perror("PTRACE_POKEDATA");
	       	return 0;
        }
       	
		(*lpNumberOfBytesWritten) += sizeof(pokeByte);
    }

	errno = 0;
	if(ptrace(PTRACE_DETACH, pid, NULL, NULL) == -1){
		perror("PTRACE_DEATCH");
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
		
	*nStartAddress = phdr.p_vaddr;
	*nEndAddress = phdr.p_vaddr + phdr.p_memsz;
										
	return ((*nEndAddress) - (*nStartAddress)) > 0;
}