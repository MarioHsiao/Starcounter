# level1/src/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(Starcounter.ErrorCodes/scerrres)
add_definitions(-D_CRT_SECURE_NO_WARNINGS)

add_subdirectory(Starcounter.ErrorCodes)
add_subdirectory(Chunks)
