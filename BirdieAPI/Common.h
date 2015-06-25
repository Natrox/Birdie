#ifndef BIRDIEAPI_COMMON_H
#define BIRDIEAPI_COMMON_H

// This header contains common functionality

#define BIRDIE_STATIC_CALL(f, args) \
	class StaticCaller_ ##f { public: StaticCaller_ ##f () { ##f args ; } }; static StaticCaller_ ##f ___StaticCaller_ ##f ;

#endif