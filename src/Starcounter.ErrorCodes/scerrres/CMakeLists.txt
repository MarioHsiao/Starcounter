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

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
  scerrres.h
  ../errorcodes.xml
)
set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
# add_dependencies(scerrres Generate_SCERRRES_H)
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
