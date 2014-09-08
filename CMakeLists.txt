# level1/CMakeLists.txt
#

cmake_minimum_required(VERSION 2.8.10)
project(level1)
enable_testing()


# Note: To avoid code duplication, this
# project relies on cmake scripts provided
# by the superbuild parent project,
# specifically CSharp helpers.
if(NOT CSHARP_FOUND)
  message(ERROR "Parent project must provide CSharp")
endif()

add_subdirectory(src/Starcounter.ErrorCodes)
