#include "BirdieExt.hpp"

#include "Common.h"

CRITICAL_SECTION	  g_csSet;
std::set<const char*> g_setOfCustomTypes;

BIRDIEAPI bool Birdie_IsTypeRegistered(const char* type)
{
	EnterCriticalSection(&g_csSet);
	bool contains = g_setOfCustomTypes.find(type) != g_setOfCustomTypes.end();
	LeaveCriticalSection(&g_csSet);

	return contains;
}

BIRDIEAPI void Birdie_AddRegisteredType(const char* type)
{
	EnterCriticalSection(&g_csSet);
	g_setOfCustomTypes.insert(type);
	LeaveCriticalSection(&g_csSet);
}

void Initialize()
{
	InitializeCriticalSection(&g_csSet);
}

BIRDIE_STATIC_CALL(Initialize, ());