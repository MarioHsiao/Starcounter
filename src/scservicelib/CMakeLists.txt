# level1/src/scservicelib/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(../../../level0/src/include)

set(scservicelib_SOURCE_FILES
    config.cpp
    logging.cpp
    main.cpp
    win32.cpp
    win32_acl.cpp
    scservicelib.def
)
set(scservicelib_HEADER_FILES
    internal.hpp
    rapidxml.hpp
)

add_library(scservicelib SHARED ${scservicelib_SOURCE_FILES} ${scservicelib_HEADER_FILES})
target_link_libraries(scservicelib
    sccoredbg
    sccorelib
    sccorelog
)
set_property(TARGET scservicelib PROPERTY FOLDER "level1")

