# level1/src/Chunks/BMX/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_library(bmx SHARED
    bmx.cpp
    bmx.def
    bmx.hpp
    bmx_manager.cpp
    chunk_helper.h
    utils.cpp
)
