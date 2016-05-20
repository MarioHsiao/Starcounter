# level1/src/Chunks/BMX/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(../../../../level0/src/include)

add_library(bmx SHARED
    bmx.cpp
    bmx.hpp
    chunk_helper.h
    utils.cpp
    bmx.def
)
target_link_libraries(bmx
    sccoredb
    sccoredbg
    coalmine
    sccorelib
    sccorelog
)
set_property(TARGET bmx PROPERTY FOLDER "level1/Chunks")
