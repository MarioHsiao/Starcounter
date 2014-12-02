# level1/src/HTTP/HttpParser/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(
    OurHeaders
    ThirdPartyHeaders
	../../../../level0/src/include
)

set(HttpParser_SOURCE_FILES
    OurSources/apps_http_parsers.cpp
    OurSources/apps_http_request_parser.cpp
    OurSources/apps_http_response_parser.cpp
    ThirdPartySources/http_parser.c
    HttpParser.def
)

set(HttpParser_HEADER_FILES
    OurHeaders/http_common.hpp
    OurHeaders/http_request.hpp
    OurHeaders/http_response.hpp
    ThirdPartyHeaders/http_parser.h
)

add_library(HttpParser SHARED ${HttpParser_SOURCE_FILES} ${HttpParser_HEADER_FILES})
OrganizeVisualStudio(HttpParser) # "level1/HTTP"

# HTTP\HttpParser\HttpParser.vcxproj
# HTTP\HttpParser\HttpParser32.vcxproj
