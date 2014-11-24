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


if(WIN32)
    add_custom_command(
      OUTPUT
        scerrres.h
#      COMMAND
#        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -cs
      COMMAND
        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -mc scerrres.mc
      COMMAND
        mc.exe -r .\\ -v scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      MAIN_DEPENDENCY
        ${CMAKE_CURRENT_SOURCE_DIR}/../errorcodes.xml
      DEPENDS
        ${CMAKE_CURRENT_SOURCE_DIR}/../errorcodes.xml
        scerrcc
      COMMENT
        "Generating scerrres.h"
    )
endif()

add_custom_target(Generate_SCERRRES_H
    DEPENDS
        scerrres.h
        scerrcc
    COMMENT
        "Triggered target Generate_SCERRRES_H"
)
set_property(TARGET Generate_SCERRRES_H PROPERTY FOLDER "level1/Starcounter.ErrorCodes")

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
  scerrres.h
  ../errorcodes.xml
)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
add_dependencies(scerrres Generate_SCERRRES_H)
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
