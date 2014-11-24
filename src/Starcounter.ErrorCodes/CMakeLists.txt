# level1/src/Starcounter.ErrorCodes/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

find_package(CSharp)
include(UseCSharp)

add_subdirectory(Starcounter.Errors)
add_subdirectory(ErrorCodeCompiler)
add_subdirectory(scerrres)
