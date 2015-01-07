# level1/src/Starcounter.ErrorCodes/scerrres/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

if(MSVC)
    set(msvc_compile_flags "/Zp8 /MP /Gm- /GS /GL /fp:precise")
    foreach(flag_var_prefix CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
        string(REPLACE "/MD" "/MT" ${flag_var_prefix} "${${flag_var_prefix}}")
        if(NOT ${flag_var_prefix} MATCHES "/GL")
            set(${flag_var_prefix} "${${flag_var_prefix}} ${msvc_compile_flags}")
        endif()
        foreach(flag_var_suffix _DEBUG _RELEASE _MINSIZEREL _RELWITHDEBINFO)
            string(REPLACE "/MD" "/MT" ${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}}")
            if(NOT ${flag_var_prefix}${flag_var_suffix} MATCHES "/GL")
                set(${flag_var_prefix}${flag_var_suffix} "${${flag_var_prefix}${flag_var_suffix}} ${msvc_compile_flags}")
            endif()
        endforeach()
    endforeach()
    set(CMAKE_SHARED_LINKER_FLAGS "/SUBSYSTEM:WINDOWS /LTCG")
    add_definitions(
        -D_VARIADIC_MAX=10
        -DWIN32
        -D_WIN32
        -D_CRT_SECURE_NO_DEPRECATE
    )
endif()

# set(SC_OUTPUT_DIR ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${CMAKE_CFG_INTDIR})
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../../bin/${CMAKE_CFG_INTDIR})

add_definitions(
    -DFORMATMESSAGE_EXPORTS
    -D_DEBUG
    )

if(0) # was: WIN32
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.mc
#      COMMAND
#        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -cs
      COMMAND
        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME>
      ARGS
        ..\\errorcodes.xml -mc ${SC_OUTPUT_DIR}/scerrres.mc -ch ${SC_OUTPUT_DIR}/scerrres.h -cc ${SC_OUTPUT_DIR}/scerrres.c
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
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.c
      COMMAND
        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME>
      ARGS
        ..\\errorcodes.xml -ch ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h -cc ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.c
      DEPENDS
        scerrcc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      COMMENT
        "Generating scerrres.h and scerrres.c"
      VERBATIM
    )
endif()

add_custom_target(Generate_SCERRRES_H
    DEPENDS
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
)
OrganizeVisualStudio(Generate_SCERRRES_H)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.c PROPERTIES GENERATED 1)

set(scerrres_SOURCES
  Format.cpp
  format.h
  Format.def
  ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
)

if(0) # was: WIN32
    set(scerrres_SOURCES
        ${scerrres_SOURCES}
        Format.def
        ${SC_OUTPUT_DIR}/scerrres.res
    )
    set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.res PROPERTIES GENERATED 1)
    set(scerrres_LINKFLAGS "/DEBUG ${SC_OUTPUT_DIR}/scerrres.res")
else()
    set(scerrres_LINKFLAGS)
endif()

add_library(scerrres SHARED
    ${scerrres_SOURCES}
)

OrganizeVisualStudio(scerrres) # "level1/Starcounter.ErrorCodes"
# set_target_properties(scerrres PROPERTIES LINK_FLAGS ${scerrres_LINKFLAGS})
add_dependencies(scerrres scerrcc)
