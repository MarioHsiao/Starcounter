# level1/src/Starcounter.ErrorCodes/ErrorCodeCompiler/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.8)

# The ErrorCodeCompiler (scerrcc) is unconditionally built to the same source dir by Blue.sln
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR})
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

get_property(starcounter_errors_dll TARGET Starcounter.Errors PROPERTY OUTPUT_NAME)

csharp_add_executable(scerrcc
    System.Web.dll
    ${starcounter_errors_dll}
    scerrcc/Program.cs
    scerrcc/CommandLine.cs
    scerrcc/Properties/AssemblyInfo.cs
)
add_dependencies(scerrcc Starcounter.Errors)
set_property(TARGET scerrcc PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
