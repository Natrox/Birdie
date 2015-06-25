#ifndef BIRDIEAPI_MAIN_H
#define BIRDIEAPI_MAIN_H

#include <stdint.h>

#include "Common.h"

// This header contains the main Birdie API functions

#ifdef BIRDIEAPI_EXPORTS
#define BIRDIEAPI extern "C" __declspec(dllexport)
#else
#define BIRDIEAPI extern "C" __declspec(dllimport)
#endif

typedef void* (*BIRDIE_ALLOCATION_FUNCTION)(size_t);
typedef void(*BIRDIE_DEALLOCATION_FUNCTION)(void*);

typedef uint32_t BIRDIE_HANDLE;
typedef BIRDIE_HANDLE *LPBIRDIE_HANDLE;

typedef int BIRDIE_ERROR;
typedef const char* BIRDIE_TYPE;

#define BIRDIE_TYPE_BOOL        "Bool"
#define BIRDIE_TYPE_INT8	    "Int8"
#define BIRDIE_TYPE_INT16	    "Int16"
#define BIRDIE_TYPE_INT32	    "Int32"
#define BIRDIE_TYPE_INT64	    "Int64"
#define BIRDIE_TYPE_UINT8	    "UInt8"
#define BIRDIE_TYPE_UINT16	    "UInt16"
#define BIRDIE_TYPE_UINT32	    "UInt32"
#define BIRDIE_TYPE_UINT64	    "UInt64"
#define BIRDIE_TYPE_FLOAT32	    "Float32"
#define BIRDIE_TYPE_FLOAT64	    "Float64"
#define BIRDIE_TYPE_ANSI_STRING "ANSIString"
#define BIRDIE_TYPE_UTF8_STRING "UTF8String"
#define BIRDIE_TYPE_HEX_PATTERN "HEXPattern"

#define BIRDIE_SIZE_BOOL        sizeof(bool)
#define BIRDIE_SIZE_INT8	    sizeof(int8_t)
#define BIRDIE_SIZE_INT16	    sizeof(int16_t)
#define BIRDIE_SIZE_INT32	    sizeof(int32_t)
#define BIRDIE_SIZE_INT64	    sizeof(int64_t)
#define BIRDIE_SIZE_UINT8	    sizeof(uint8_t)
#define BIRDIE_SIZE_UINT16	    sizeof(uint16_t)
#define BIRDIE_SIZE_UINT32	    sizeof(uint32_t)
#define BIRDIE_SIZE_UINT64	    sizeof(uint64_t)
#define BIRDIE_SIZE_FLOAT32	    sizeof(float)
#define BIRDIE_SIZE_FLOAT64	    sizeof(double)

typedef enum
{
	BIRDIE_SUCCESS = 0,
	BIRDIE_ERROR_COULD_NOT_CONNECT,
	BIRDIE_ERROR_INSUFFICIENT_MEMORY,
	BIRDIE_ERROR_INVALID_PARAMS,
	BIRDIE_ERROR_TYPE_PREEXISTING,
	BIRDIE_ERROR_NOT_CONNECTED
} BIRDIE_ERRORS;

typedef struct
{
	const char* name;
	BIRDIE_TYPE type;
	void*       offset;
	size_t      size;
} BIRDIE_TEMPLATE_ENTRY;


// Control functions

/// <summary>
///		Sets the memory handlers which the Birdie API will use to make memory allocations.
///		If not specified, malloc() and free() are used. This needs to be called before initialization.
/// </summary>
/// <param name="allocationFunction">
///		A allocation function which returns "void*" and accepts "size_t".
/// </param>
/// <param name="deallocationFunction">
///		An deallocation function which accepts "void*".
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_INVALID_PARAMS when either parameter is NULL.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_SetMemoryHandlers(BIRDIE_ALLOCATION_FUNCTION allocationFunction, BIRDIE_DEALLOCATION_FUNCTION deallocationFunction);

/// <summary>
///		Initializes the global Birdie API context and tries to connect to the Birdie tool.
/// </summary>
/// <param name="challengeKey">
///		A 64bit key that is matched against the key set by the tool.
///		If this key does not match, the tool will not accept any subsequent data.
/// </param>
/// <param name="pAddress">
///		The IP address or resolvable host name of the tool.
/// </param>
/// <param name="pPort">
///		The port the tool is listening on.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_COULD_NOT_CONNECT upon failure,
///		please use WSAGetLastError for more detailed WinSock error information.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_Initialize(uint64_t challengeKey, const char* pAddress, const char* pPort);

/// <summary>
///		Terminates any active connection to the Birdie tool.
/// </summary>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool 
///     (this can be used for diagnostic purposes).
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_Terminate(void);


// Watch functions

/// <summary>
///		Creates a new watch category.
///		A valid handle is returned, even if there is no connection.
/// </summary>
/// <param name="pName">
///		Name of the category.
/// </param>
/// <param name="parent">
///		Handle to a parent for this category. Optional, use '0' for no parent.
/// </param>
/// <param name="pHandle">
///		Pointer to a handle object in which the new watch category handle is stored.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_INSUFFICIENT_MEMORY if there was insufficient temporary space.
///		Returns BIRDIE_ERROR_INVALID_PARAMS if one or more parameters were malformed.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_AddWatchCategory(const char* pName, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle);

/// <summary>
///		Removes a watch category.
///     This will also remove all child watch objects.
/// </summary>
/// <param name="handle">
///		A handle to the category that is to be deleted.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_RemoveWatchCategory(BIRDIE_HANDLE handle);

/// <summary>
///		Creates a new watch.
///		A valid handle is returned, even if there is no connection.
/// </summary>
/// <param name="pName">
///		Name of the watch.
/// </param>
/// <param name="type">
///		Determines how the memory is formatted in the tool. Use one of the BIRDIE_TYPE_* types, or a custom type if
///		implemented by the version of the tool.
/// </param>
/// <param name="pBase">
///		Address of a section of memory to monitor. 
/// </param>
/// <param name="dataSizeBytes">
///		Determines the size in bytes to monitor. Use one of the BIRDIE_SIZE_* values if available.
///		For strings and data using BIRDIE_TYPE_HEX, use an appropiate size value.
/// </param>
/// <param name="parent">
///		Handle to a parent. This can be a category, or a watch object. Optional, use '0' for no parent.
/// </param>
/// <param name="pHandle">
///		Pointer to a handle object in which the new watch handle is stored.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_INSUFFICIENT_MEMORY if there was insufficient temporary space.
///		Returns BIRDIE_ERROR_INVALID_PARAMS if one or more parameters were malformed.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_AddWatch(const char* pName, BIRDIE_TYPE type, void* pBase, size_t dataSizeBytes, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle);

/// <summary>
///		Removes a watch.
///     This will also remove all child watch objects.
/// </summary>
/// <param name="handle">
///		A handle to the watch that is to be deleted.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_RemoveWatch(BIRDIE_HANDLE handle);


// Log functions

/// <summary>
///		Logs a message to the tool.
/// </summary>
/// <param name="pFilter">
///		Optional filter, can be used to categorize. Use null or "" for no filter.
/// </param>
/// <param name="pMessage">
///		Pre-formatted message that is sent to the tool.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_INSUFFICIENT_MEMORY if there was insufficient temporary space.
///		Returns BIRDIE_ERROR_INVALID_PARAMS if one or more parameters were malformed.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_Log(const char* pFilter, const char* pMessage);

/// <summary>
///		Logs a formatted message to the tool.
/// </summary>
/// <param name="pFilter">
///		Optional filter, can be used to categorize. Use null or "" for no filter.
/// </param>
/// <param name="pFormat">
///		Format of the message that is sent to the tool.
/// </param>
/// <param name="...">
///		Objects to use in the formatting routine.
/// </param>
/// <returns>
///		Returns BIRDIE_SUCCESS on success.
///		Returns BIRDIE_ERROR_INSUFFICIENT_MEMORY if there was insufficient temporary space.
///		Returns BIRDIE_ERROR_INVALID_PARAMS if one or more parameters were malformed.
///		Returns BIRDIE_ERROR_NOT_CONNECTED if there is no connection to the tool.
/// </returns>
BIRDIEAPI BIRDIE_ERROR Birdie_LogF(const char* pFilter, const char* pFormat, ...);

#endif