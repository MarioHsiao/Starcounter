# level1/src/scservicelib/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(
	../../../level0/src/include
)

add_definitions(-D_UNICODE -DUNICODE)

set(scservice_SOURCE_FILES
    config.cpp
    logging.cpp
    main.cpp
    win32.cpp
    win32_acl.cpp
    scservicelib.def
)
set(scservice_HEADER_FILES
    internal.hpp
    rapidxml.hpp
)

add_library(scservice SHARED ${scservice_SOURCE_FILES} ${scservice_HEADER_FILES})
target_link_libraries(scservice
    sccoredbg
#    sccorelib
    sccorelog
	scerrres
	urihelp
)
set_property(TARGET scservice PROPERTY FOLDER "level1")

