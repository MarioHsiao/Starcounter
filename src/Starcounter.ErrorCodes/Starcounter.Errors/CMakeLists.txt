# level1/src/Starcounter.ErrorCodes/Starcounter.Errors/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

# The Starcounter.Errors.dll is unconditionally built to the same source dir by Blue.sln
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../ErrorCodeCompiler)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR})
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${SC_OUTPUT_DIR})
if(CMAKE_CONFIGURATION_TYPES)
foreach(uod_cfg ${CMAKE_CONFIGURATION_TYPES})
  string(TOUPPER ${uod_cfg} uod_cfg_uc)
  set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR})
  set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR})
  set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${uod_cfg_uc} ${SC_OUTPUT_DIR})
endforeach()
endif()
link_directories(${CMAKE_ARCHIVE_OUTPUT_DIRECTORY})

find_package(CSharp)
include(UseCSharp)

csharp_add_library(Starcounter.Errors
    ErrorCode.cs
    ErrorFile.cs
    ErrorFileReader.cs
    Facility.cs
    Severity.cs
)
OrganizeVisualStudio(Starcounter.Errors)
# set_property(TARGET Starcounter.Errors PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
