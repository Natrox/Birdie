#include "Birdie.h"

#include <stdio.h>
#include <WinSock2.h>
#include <WS2tcpip.h>

// 256k
#define BIRDIE_SCRATCH_BUFFER_SIZE 262144

typedef int WSA_ERROR;

typedef enum
{
	RegisterProcess = 1,
	AddWatch = 2,
	RemoveWatchObject = 3,
	AddCategory = 4,
	AddLogMessage = 5,
	AddCustomTypeHandler = 6
} BIRDIE_OPERATION_TYPE;


// Global data used for the Birdie tool connection.

static BIRDIE_ALLOCATION_FUNCTION	  g_allocFunction = malloc;
static BIRDIE_DEALLOCATION_FUNCTION  g_deallocFunction = free;

static SOCKET				  g_toolSocket = INVALID_SOCKET;
static bool					  g_isConnected = false;
static volatile BIRDIE_HANDLE g_handleCounter = 0;

static WSAData				  g_wsaData;
static CRITICAL_SECTION       g_csBuffer;
static char*				  g_topScratchBuffer = NULL;
static char*				  g_scratchBuffer = NULL;


// Prototypes

BIRDIE_HANDLE Birdie_GetNewHandle();
WSA_ERROR Birdie_SendData(size_t size, bool sendChunkSize = true);
BIRDIE_ERROR Birdie_RemoveWatchObject(BIRDIE_HANDLE handle);

// Header fuction implementations

BIRDIEAPI BIRDIE_ERROR Birdie_SetMemoryHandlers(BIRDIE_ALLOCATION_FUNCTION allocationFunction, BIRDIE_DEALLOCATION_FUNCTION deallocationFunction)
{
	if (allocationFunction == NULL || deallocationFunction == NULL)
		return BIRDIE_ERROR_INVALID_PARAMS;

	g_allocFunction = allocationFunction;
	g_deallocFunction = deallocationFunction;

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_Initialize(uint64_t challengeKey, const char* pAddress, const char* pPort)
{
	// Initialize WinSock
	WSA_ERROR wsaError = WSAStartup(MAKEWORD(2, 0), &g_wsaData);

	if (wsaError != 0)
		return BIRDIE_ERROR_COULD_NOT_CONNECT;

	addrinfo hints;

	memset(&hints, 0, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;

	addrinfo* serverInfo;
	WSA_ERROR getAddrInfoError = getaddrinfo(pAddress, pPort, &hints, &serverInfo);

	if (getAddrInfoError != 0)
		return BIRDIE_ERROR_COULD_NOT_CONNECT;

	addrinfo* connection = NULL;

	// Loop through network interfaces to find one that can make a valid connection
	for (connection = serverInfo; connection != NULL; connection = connection->ai_next)
	{
		if ((g_toolSocket = socket(connection->ai_family, connection->ai_socktype, connection->ai_protocol)) == INVALID_SOCKET)
			continue;

		if (connect(g_toolSocket, connection->ai_addr, (int)connection->ai_addrlen) != 0)
		{
			shutdown(g_toolSocket, SD_BOTH);
			continue;
		}

		break;
	}

	if (connection == NULL)
	{
		g_toolSocket = INVALID_SOCKET;
		return BIRDIE_ERROR_COULD_NOT_CONNECT;
	}

	// Disable Nagle's algorithm, failure should not be fatal
	int optVal = 1;
	setsockopt(g_toolSocket, IPPROTO_TCP, TCP_NODELAY, (const char*)&optVal, (int) sizeof(optVal));

	freeaddrinfo(serverInfo);

	// We're connected, let's initialize all of the other stuff
	g_handleCounter = 0;
	g_isConnected = true;

	InitializeCriticalSection(&g_csBuffer);

	// Lets initialize the scratch buffers
	g_topScratchBuffer = (char*)g_allocFunction(BIRDIE_SCRATCH_BUFFER_SIZE + sizeof(uint32_t));
	g_scratchBuffer = g_topScratchBuffer + sizeof(uint32_t);

	// Now, send the challenge
	memcpy((void*)g_scratchBuffer, &challengeKey, sizeof(uint64_t));
	Birdie_SendData(sizeof(uint64_t), false);

	// And the process info
	uint64_t processId = (uint64_t)GetCurrentProcessId();

	size_t offset = 0;
	uint32_t operationType = RegisterProcess;

	memcpy((void*)(g_scratchBuffer + offset), &operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), &processId, sizeof(uint64_t));
	offset += sizeof(uint64_t);

	Birdie_SendData(offset);

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_Terminate(void)
{
	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	// Shut down our socket and WinSock
	shutdown(g_toolSocket, SD_BOTH);
	closesocket(g_toolSocket);
	WSACleanup();

	// Clean up the extra stuff
	// Enter our buffer critical section in order to make sure it's removable
	EnterCriticalSection(&g_csBuffer);
	LeaveCriticalSection(&g_csBuffer);
	DeleteCriticalSection(&g_csBuffer);

	// Only need to clear the top, since the other one is an alias
	g_deallocFunction(g_topScratchBuffer);

	g_handleCounter = 0;

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_AddWatchCategory(const char* pName, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle)
{
	if (pHandle == NULL || pName == NULL)
		return BIRDIE_ERROR_INVALID_PARAMS;

	*pHandle = Birdie_GetNewHandle();

	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	size_t nameLength = strlen(pName);

	if (nameLength == 0)
		return BIRDIE_ERROR_INVALID_PARAMS;

	size_t totalSize =
		sizeof(uint32_t) +
		sizeof(uint32_t) +
		nameLength +
		sizeof(BIRDIE_HANDLE) +
		sizeof(BIRDIE_HANDLE);

	if (totalSize > BIRDIE_SCRATCH_BUFFER_SIZE)
		return BIRDIE_ERROR_INSUFFICIENT_MEMORY;

	size_t offset = 0;
	uint32_t operationType = AddCategory;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&nameLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)pName, nameLength);
	offset += nameLength;

	memcpy((void*)(g_scratchBuffer + offset), (void*)&parent, sizeof(BIRDIE_HANDLE));
	offset += sizeof(BIRDIE_HANDLE);

	memcpy((void*)(g_scratchBuffer + offset), (void*)pHandle, sizeof(BIRDIE_HANDLE));
	offset += sizeof(BIRDIE_HANDLE);

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_RemoveWatchCategory(BIRDIE_HANDLE handle)
{
	return Birdie_RemoveWatchObject(handle);
}

BIRDIEAPI BIRDIE_ERROR Birdie_AddWatch(const char* pName, BIRDIE_TYPE type, void* pBase, size_t dataSizeBytes, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle)
{
	if (pName == NULL || type == NULL || pBase == NULL || dataSizeBytes == 0)
		return BIRDIE_ERROR_INVALID_PARAMS;

	BIRDIE_HANDLE newHandle = Birdie_GetNewHandle();

	if (pHandle)
		*pHandle = newHandle;

	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	size_t nameLength = strlen(pName);

	if (nameLength == 0)
		return BIRDIE_ERROR_INVALID_PARAMS;

	size_t typeLength = strlen(type);

	if (typeLength == 0)
		return BIRDIE_ERROR_INVALID_PARAMS;

	size_t totalSize =
		sizeof(uint32_t) +
		sizeof(uint32_t) +
		nameLength +
		sizeof(uint32_t) +
		typeLength +
		sizeof(BIRDIE_HANDLE) +
		sizeof(BIRDIE_HANDLE) +
		sizeof(uint64_t) +
		sizeof(uint32_t);

	if (totalSize > BIRDIE_SCRATCH_BUFFER_SIZE)
		return BIRDIE_ERROR_INSUFFICIENT_MEMORY;

	uint64_t basePtr64 = (uint64_t)pBase;

	size_t offset = 0;
	uint32_t operationType = AddWatch;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&typeLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)type, typeLength);
	offset += typeLength;

	memcpy((void*)(g_scratchBuffer + offset), (void*)&nameLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)pName, nameLength);
	offset += nameLength;

	memcpy((void*)(g_scratchBuffer + offset), (void*)&parent, sizeof(BIRDIE_HANDLE));
	offset += sizeof(BIRDIE_HANDLE);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&newHandle, sizeof(BIRDIE_HANDLE));
	offset += sizeof(BIRDIE_HANDLE);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&basePtr64, sizeof(uint64_t));
	offset += sizeof(uint64_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&dataSizeBytes, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_RemoveWatch(BIRDIE_HANDLE handle)
{
	return Birdie_RemoveWatchObject(handle);
}

BIRDIEAPI BIRDIE_ERROR Birdie_Log(const char* pFilter, const char* pMessage)
{
	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	if (pMessage == NULL)
		return BIRDIE_ERROR_INVALID_PARAMS;

	const char* filter = "";

	if (pFilter != NULL)
		filter = pFilter;

	size_t filterLength = strlen(filter);
	size_t messageLength = strlen(pMessage);

	size_t totalSize =
		sizeof(uint32_t) +
		sizeof(uint32_t) +
		filterLength +
		sizeof(uint32_t) +
		messageLength;

	if (totalSize > BIRDIE_SCRATCH_BUFFER_SIZE)
		return BIRDIE_ERROR_INSUFFICIENT_MEMORY;

	size_t offset = 0;
	uint32_t operationType = AddLogMessage;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&messageLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)pMessage, messageLength);
	offset += messageLength;

	memcpy((void*)(g_scratchBuffer + offset), (void*)&filterLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)filter, filterLength);
	offset += filterLength;

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}

BIRDIEAPI BIRDIE_ERROR Birdie_LogF(const char* pFilter, const char* pFormat, ...)
{
	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	if (pFormat == NULL)
		return BIRDIE_ERROR_INVALID_PARAMS;

	const char* filter = "";

	if (pFilter != NULL)
		filter = pFilter;

	size_t filterLength = strlen(filter);
	size_t formatLength = strlen(pFormat);

	size_t approximateTotalSize =
		sizeof(uint32_t) +
		sizeof(uint32_t) +
		filterLength +
		sizeof(uint32_t) +
		formatLength * 2;

	if (approximateTotalSize > BIRDIE_SCRATCH_BUFFER_SIZE)
		return BIRDIE_ERROR_INSUFFICIENT_MEMORY;

	size_t offset = 0;
	size_t messageLengthOffset = 0;
	size_t messageLength = 0;
	uint32_t operationType = AddLogMessage;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	// Skip message length for now
	messageLengthOffset = offset;
	offset += sizeof(uint32_t);

	va_list args;
	va_start(args, pFormat);

	messageLength = (size_t)vsprintf((char*)(g_scratchBuffer + offset), pFormat, args);
	va_end(args);

	offset += messageLength;

	memcpy((void*)(g_scratchBuffer + messageLengthOffset), (void*)&messageLength, sizeof(uint32_t));

	memcpy((void*)(g_scratchBuffer + offset), (void*)&filterLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)filter, filterLength);
	offset += filterLength;

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}

BIRDIE_ERROR Birdie_AddCustomTypeHandler(BIRDIE_TYPE type, const char* handlerCode)
{
	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	uint32_t typeLength = (uint32_t)strlen(type);
	uint32_t codeLength = (uint32_t)strlen(handlerCode);

	size_t totalSize = 
		sizeof(uint32_t) + 
		sizeof(uint32_t) +
		sizeof(uint32_t) +
		typeLength +
		sizeof(uint32_t) +
		codeLength;

	if (totalSize > BIRDIE_SCRATCH_BUFFER_SIZE)
		return BIRDIE_ERROR_INSUFFICIENT_MEMORY;

	size_t offset = 0;
	uint32_t operationType = AddCustomTypeHandler;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&typeLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)type, typeLength);
	offset += typeLength;

	memcpy((void*)(g_scratchBuffer + offset), (void*)&codeLength, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)handlerCode, codeLength);
	offset += codeLength;

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}

BIRDIE_HANDLE Birdie_GetNewHandle()
{
	return (BIRDIE_HANDLE)InterlockedIncrement(&g_handleCounter);
}

WSA_ERROR Birdie_SendData(size_t size, bool sendChunkSize)
{
	if (!g_isConnected)
		return SOCKET_ERROR;

	size_t startOffset = 0;
	size_t totalSize = size;

	if (sendChunkSize)
	{
		totalSize += sizeof(uint32_t);
		*((uint32_t*)g_topScratchBuffer) = (uint32_t)size;
	}
	else
		startOffset = sizeof(uint32_t);

	size_t bytesRemaining = totalSize;

	while (bytesRemaining > 0)
	{
		WSA_ERROR bytesSent = send(g_toolSocket, (const char*)(g_topScratchBuffer + startOffset + (totalSize - bytesRemaining)), (int)bytesRemaining, 0);

		if (bytesSent == SOCKET_ERROR)
			return WSAGetLastError();

		bytesRemaining -= (size_t)bytesSent;
	}

	return 0;
}

BIRDIE_ERROR Birdie_RemoveWatchObject(BIRDIE_HANDLE handle)
{
	if (g_isConnected == false)
		return BIRDIE_ERROR_NOT_CONNECTED;

	size_t offset = 0;
	uint32_t operationType = RemoveWatchObject;

	EnterCriticalSection(&g_csBuffer);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&operationType, sizeof(uint32_t));
	offset += sizeof(uint32_t);

	memcpy((void*)(g_scratchBuffer + offset), (void*)&handle, sizeof(BIRDIE_HANDLE));
	offset += sizeof(BIRDIE_HANDLE);

	WSA_ERROR wsaError = Birdie_SendData(offset);

	LeaveCriticalSection(&g_csBuffer);

	if (wsaError != 0)
		return BIRDIE_ERROR_NOT_CONNECTED;

	return BIRDIE_SUCCESS;
}