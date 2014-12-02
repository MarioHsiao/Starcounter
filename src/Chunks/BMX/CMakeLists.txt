# level1/src/Chunks/BMX/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(../../../../level0/src/include)

add_library(bmx
    bmx.cpp
    bmx.hpp
    bmx_manager.cpp
    chunk_helper.h
    utils.cpp
    bmx.def
)
target_link_libraries(bmx
    sccoredb
    sccoredbg
    coalmine
    sccorelog
)
OrganizeVisualStudio(bmx) # "level1/Chunks"
