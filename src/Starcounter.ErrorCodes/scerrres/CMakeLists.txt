# level1/src/Starcounter.ErrorCodes/scerrres/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

if(MSVC)
    foreach(flag_var_prefix CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
        string(REPLACE "/MD" "/MT" ${flag_var_prefix} "${${flag_var_prefix}}")
        foreach(flag_var_suffix _DEBUG _RELEASE _MINSIZEREL _RELWITHDEBINFO)
            string(REPLACE "/MD" "/MT" ${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}}")
        endforeach()
    endforeach()
endif()

# The scerrres library is unconditionally built to ../../../bin/$(Configuration) by Blue.sln
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../../bin)
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

if(WIN32)
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.mc
#      COMMAND
#        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -cs
      COMMAND
        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -mc ..\\scerrres\\scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}/../ErrorCodeCompiler
      MAIN_DEPENDENCY
        ${CMAKE_CURRENT_SOURCE_DIR}/../errorcodes.xml
      DEPENDS
        scerrcc
        ${CMAKE_CURRENT_SOURCE_DIR}/../errorcodes.xml
        # $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME>
      COMMENT
        "Generating scerrres.mc"
    )
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
      COMMAND
        mc.exe -r .\\ -v scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.mc
      COMMENT
        "Generating scerrres.h"
    )
else()
    message(SEND_ERROR "implement scerrres.h generation for Linux")
endif()

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
  ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
