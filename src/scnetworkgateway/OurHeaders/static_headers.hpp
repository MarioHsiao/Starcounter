#pragma once
#ifndef STATIC_HEADERS_HPP
#define STATIC_HEADERS_HPP

// Standard headers.
#include <iostream>
#include <sstream>
#include <fstream>
#include <vector>
#include <iterator>
#include <algorithm>
#include <iomanip>
#include <limits>
#include <list>
#include <cstdint>
#include <bitset>

// Windows headers.
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <mswsock.h>
#include <mmsystem.h>
#include <strsafe.h>
#undef WIN32_LEAN_AND_MEAN

#include <conio.h>
#include <wtypes.h>

// Internal foreign headers.
#include "../../HTTP/HttpParser/ThirdPartyHeaders/http_parser.h"
#include <rapidxml.hpp>
#include <cdecode.h>
#include <cencode.h>
#include <sha-1.h>

#endif // STATIC_HEADERS_HPP