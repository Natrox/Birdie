#ifndef BIRDIEAPI_EXT_CPP_H
#define BIRDIEAPI_EXT_CPP_H

#include "Birdie.h"

#include <Windows.h>

#include <cstring>
#include <set>
#include <typeinfo.h>

// This header contains extended Birdie functionality, exclusive to C++

BIRDIEAPI bool Birdie_IsTypeRegistered(const char* type);
BIRDIEAPI void Birdie_AddRegisteredType(const char* type);


template <typename T> 
class BirdieTypeGetter              
{ 
public: 
	static BIRDIE_TYPE GetType() 
	{ 
		// Determine if we have a custom type or not
		const char* customTypeName = typeid(T).name();
		bool contains = Birdie_IsTypeRegistered(customTypeName);

		if (contains)
			return customTypeName;

		return BIRDIE_TYPE_HEX_PATTERN;
	} 
};

template <>           class BirdieTypeGetter<bool>        { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_BOOL;        } };
template <>           class BirdieTypeGetter<int8_t>      { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_INT8;        } };
template <>           class BirdieTypeGetter<int16_t>     { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_INT16;       } };
template <>           class BirdieTypeGetter<int32_t>     { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_INT32;       } };
template <>           class BirdieTypeGetter<int64_t>     { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_INT64;       } };
template <>           class BirdieTypeGetter<uint8_t>     { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_UINT8;       } };
template <>           class BirdieTypeGetter<uint16_t>    { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_UINT16;      } };
template <>           class BirdieTypeGetter<uint32_t>    { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_UINT32;      } };
template <>           class BirdieTypeGetter<uint64_t>    { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_UINT64;      } };
template <>           class BirdieTypeGetter<float>       { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_FLOAT32;     } };
template <>           class BirdieTypeGetter<double>      { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_FLOAT64;     } };
template <>           class BirdieTypeGetter<const char*> { public: static BIRDIE_TYPE GetType() { return BIRDIE_TYPE_ANSI_STRING; } };

template <typename T> class BirdieSizeGetter              { public: static size_t GetSize(T* obj)           { return sizeof(T);    } };
template <>           class BirdieSizeGetter<const char*> { public: static size_t GetSize(const char* obj)  { return strlen(obj);  } };


template <typename T>
BIRDIE_ERROR Birdie_RegisterCustomType(const char* customHandlerCode)
{
	if (customHandlerCode == NULL)
		return BIRDIE_ERROR_INVALID_PARAMS;

	// Check for existing type
	// First, check the base types
	BIRDIE_TYPE type = BirdieTypeGetter<T>::GetType();

	// If type is not HEX_PATTERN, it already exists in the base type list
	if (strcmp(type, BIRDIE_TYPE_HEX_PATTERN) != 0)
		return BIRDIE_ERROR_TYPE_PREEXISTING;

	const char* customTypeName = typeid(T).name();

	// Now check the custom type list
	bool contains = Birdie_IsTypeRegistered(customTypeName);

	if (contains)
		return BIRDIE_ERROR_TYPE_PREEXISTING;

	Birdie_AddRegisteredType(customTypeName);

	// Custom type code is sent over to Birdie
	return Birdie_AddCustomTypeHandler(type, customHandlerCode);
}

template <typename T>
static BIRDIE_ERROR Birdie_AddWatch(const char* pName, T* pBase, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle)
{
	BIRDIE_TYPE type = BirdieTypeGetter<T>::GetType();
	size_t size = BirdieSizeGetter<T>::GetSize(pBase);

	return Birdie_AddWatch(pName, type, (void*)pBase, size, parent, pHandle);
}

template <>
static BIRDIE_ERROR Birdie_AddWatch(const char* pName, const char* pBase, BIRDIE_HANDLE parent, LPBIRDIE_HANDLE pHandle)
{
	BIRDIE_TYPE type = BirdieTypeGetter<const char*>::GetType();
	size_t size = BirdieSizeGetter<const char*>::GetSize(pBase);

	return Birdie_AddWatch(pName, type, (void*)pBase, size, parent, pHandle);
}

#endif