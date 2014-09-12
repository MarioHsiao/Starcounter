# level1/src/Hosting/sccoreapp/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(sccoreapp)

set(sccoreapp_SOURCE_FILES
    init.cpp
    sccoreapp.cpp
    sccoreapp.def
)

set(sccoreapp_HEADER_FILES
    external.h
    internal.h
)
add_executable(sccoreapp ${sccoreapp_SOURCE_FILES} ${sccoreapp_HEADER_FILES})
target_link_libraries(sccoreapp
    scerrres
    bmx
)
