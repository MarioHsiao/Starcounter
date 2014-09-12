# level1/src/Hosting/sccoredbh/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(sccoredbh)

set(sccoredbh_SOURCE_FILES
    sccoredbh.cpp
    sccoredbh.def
)

set(sccoredbh_HEADER_FILES
    external.h
    internal.h
)
add_executable(sccoredbh ${sccoredbh_SOURCE_FILES} ${sccoredbh_HEADER_FILES})
target_link_libraries(sccoredbh
    scerrres
    bmx
)
