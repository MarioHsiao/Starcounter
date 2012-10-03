//
// mapped_region.cpp
// Network Gateway
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include "interprocess.hpp"

using namespace starcounter::core;


#define WIN32_LEAN_AND_MEAN
#include <Windows.h>


mapped_region::~mapped_region()
{
	if (address_)
	{
		BOOL br = UnmapViewOfFile(address_);
		address_ = 0;
	}
}

void mapped_region::init(shared_memory_object &obj)
{
	address_ = MapViewOfFile(obj.handle_, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);
}
