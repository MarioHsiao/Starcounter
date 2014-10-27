# level1/src/scnetworkgateway/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(
    ThirdPartyHeaders
    OurHeaders
	../Chunks
	../Starcounter.ErrorCodes/scerrres
	../../../level0/src/include
)

add_definitions(-D_UNICODE -DUNICODE)

set(scnetworkgateway_SOURCE_FILES
    ../HTTP/HttpParser/ThirdPartySources/http_parser.c
    OurSources/aggregation.cpp
    OurSources/gateway.cpp
    OurSources/http_proto.cpp
    OurSources/handlers.cpp
    OurSources/socket_data.cpp
    OurSources/tls_proto.cpp
    OurSources/urimatch_codegen.cpp
    OurSources/utilities.cpp
    OurSources/worker.cpp
    OurSources/worker_db_interface.cpp
    OurSources/ws_proto.cpp
    ThirdPartySources/cdecode.cpp
    ThirdPartySources/cencode.cpp
    ThirdPartySources/sha-1.cpp
    ../Chunks/common/mapped_region.cpp
    ../Chunks/common/shared_memory_object.cpp
)

set(scnetworkgateway_HEADER_FILES
    OurHeaders/gateway.hpp
    OurHeaders/http_proto.hpp
    OurHeaders/random.hpp
    OurHeaders/handlers.hpp
    OurHeaders/socket_data.hpp
    OurHeaders/tls_proto.hpp
    OurHeaders/urimatch_codegen.hpp
    OurHeaders/utilities.hpp
    OurHeaders/worker.hpp
    OurHeaders/worker_db_interface.hpp
    OurHeaders/ws_proto.hpp
    OurHeaders/static_headers.hpp
    ThirdPartyHeaders/cdecode.h
    ThirdPartyHeaders/cencode.h
    ThirdPartyHeaders/rapidxml.hpp
    ThirdPartyHeaders/sha-1.h
    ../HTTP/HttpParser/ThirdPartyHeaders/http_parser.h
)

add_executable(scnetworkgateway ${scnetworkgateway_SOURCE_FILES} ${scnetworkgateway_HEADER_FILES})
set_property(TARGET scnetworkgateway PROPERTY FOLDER "level1/scnetworkgateway")
TARGET_LINK_LIBRARIES(scnetworkgateway
	coalmine
	sccoredbg
#	sccorelib
	sccorelog
	scerrres
	urihelp
	bmx
)
add_subdirectory(GatewayToClrProxy)
