# level1/src/Starcounter.ErrorCodes/scerrres/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

if(MSVC)
    # remove "/EHsc" and add "/EHa /MP"
    foreach(flag_var_prefix CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
        if(NOT ${flag_var_prefix} MATCHES "/EHa")
            string(REPLACE "/EHsc" "" ${flag_var_prefix} "${${flag_var_prefix}}")
            set(${flag_var_prefix} "${${flag_var_prefix}} /EHa /MP")
        endif()
        foreach(flag_var_suffix _DEBUG _RELEASE _MINSIZEREL _RELWITHDEBINFO)
            if(NOT ${flag_var_prefix}${flag_var_suffix} MATCHES "/EHa")
                string(REPLACE "/EHsc" "" ${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}}")
                set(${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}} /EHa /MP")
            endif()
        endforeach()
    endforeach()
    foreach(flag_var_prefix CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
        string(REPLACE "/MD" "/MT" ${flag_var_prefix} "${${flag_var_prefix}}")
        foreach(flag_var_suffix _DEBUG _RELEASE _MINSIZEREL _RELWITHDEBINFO)
            string(REPLACE "/MD" "/MT" ${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}}")
        endforeach()
    endforeach()
endif()

# set(SC_OUTPUT_DIR ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${CMAKE_CFG_INTDIR})
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../../bin/${CMAKE_CFG_INTDIR})

add_definitions(
    -DFORMATMESSAGE_EXPORTS
    -D_DEBUG
    )

if(WIN32)
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.mc
#      COMMAND
#        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -cs
      COMMAND
        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME>
      ARGS
        ..\\errorcodes.xml -mc ${SC_OUTPUT_DIR}/scerrres.mc
      DEPENDS
        scerrcc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      COMMENT
        "Generating scerrres.mc"
      VERBATIM
    )
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
        ${SC_OUTPUT_DIR}/scerrres.rc
      COMMAND
        mc.exe
      ARGS
        -r ${SC_OUTPUT_DIR} -v ${SC_OUTPUT_DIR}/scerrres.mc
      DEPENDS
        ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      COMMENT
        "Generating scerrres.h"
      VERBATIM
    )
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.res
      COMMAND
        rc.exe
      ARGS
        scerrres.rc
      DEPENDS
        ${SC_OUTPUT_DIR}/scerrres.rc
      WORKING_DIRECTORY
        ${SC_OUTPUT_DIR}
      COMMENT
        "Generating scerrres.res"
      VERBATIM
    )
else()
    message(SEND_ERROR "implement scerrres.h generation for Linux")
endif()

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
  ${SC_OUTPUT_DIR}/scerrres.res
#  ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
#  ${SC_OUTPUT_DIR}/scerrres.rc
#  ${SC_OUTPUT_DIR}/scerrres.mc
)
#set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
#set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.mc PROPERTIES GENERATED 1)
#set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.rc PROPERTIES GENERATED 1)
set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.res PROPERTIES GENERATED 1)

add_dependencies(scerrres
    scerrcc
)

if(WIN32)
    set(scerrres_LINKFLAGS "/DEBUG ${SC_OUTPUT_DIR}/scerrres.res")
    set_target_properties(scerrres PROPERTIES LINK_FLAGS ${scerrres_LINKFLAGS})
endif()
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")

