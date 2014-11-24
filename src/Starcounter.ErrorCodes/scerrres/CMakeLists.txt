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

set(SC_OUTPUT_DIR ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${CMAKE_CFG_INTDIR})

if(WIN32)
    add_definitions(
        -D_UNICODE
        -DUNICODE
    )
    #${CSHARP_INTERPRETER} 
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.mc
        ${SC_OUTPUT_DIR}/scerrres.rc
#      COMMAND
#        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -cs
      COMMAND
        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -mc ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        scerrcc
        ${CMAKE_CURRENT_SOURCE_DIR}/../errorcodes.xml
      COMMENT
        "Generating scerrres.mc"
    )
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
      COMMAND
        mc.exe -r ${SC_OUTPUT_DIR} -v ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        ${SC_OUTPUT_DIR}/scerrres.mc
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
  ${SC_OUTPUT_DIR}/scerrres.rc
  ${SC_OUTPUT_DIR}/scerrres.mc
)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.mc PROPERTIES GENERATED 1)
set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.rc PROPERTIES GENERATED 1)
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
