# level1/src/Starcounter.ErrorCodes/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

# Match the default output directory set by Blue.sln
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../bin)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR}/${CMAKE_BUILD_TYPE})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR}/${CMAKE_BUILD_TYPE})
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR}/${CMAKE_BUILD_TYPE})
if(CMAKE_CONFIGURATION_TYPES)
    foreach(uod_cfg ${CMAKE_CONFIGURATION_TYPES})
        string(TOUPPER ${uod_cfg} uod_cfg_uc)
        set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR}/${uod_cfg})
        set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR}/${uod_cfg})
        set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR}/${uod_cfg})
    endforeach()
endif()
link_directories(${CMAKE_ARCHIVE_OUTPUT_DIRECTORY})

find_package(CSharp)
include(UseCSharp)

add_subdirectory(Starcounter.Errors)
add_subdirectory(ErrorCodeCompiler)
add_subdirectory(scerrres)
