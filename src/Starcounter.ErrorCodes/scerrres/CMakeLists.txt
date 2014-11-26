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

# set(SC_OUTPUT_DIR ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${CMAKE_CFG_INTDIR})
set(SC_OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../../../bin/${CMAKE_CFG_INTDIR})

message(SC_OUTPUT_DIR ${SC_OUTPUT_DIR})

add_definitions(
    -DFORMATMESSAGE_EXPORTS
    -D_DEBUG
    )

#    <Exec Command="..\ErrorCodeCompiler\scerrcc.exe ..\errorcodes.xml -mc $(OutDir)\$(AssemblyName).mc" />
#    <Exec Command="&quot;$(FrameworkSdkDir)\Bin\x64\mc.exe&quot; -r $(OutDir) -v $(OutDir)\$(AssemblyName).mc" />
#    <RC Source="$(OutDir)\$(AssemblyName).rc" AdditionalOptions="/V" />
#    <CL Sources="Format.cpp" AdditionalOptions="/c /D FORMATMESSAGE_EXPORTS /Fo$(OutDir)\$(AssemblyName).obj /DEBUG /Zi /Fd$(OutDir)\$(AssemblyName).pdb" />
#    <LINK Sources="$(OutDir)\$(AssemblyName).res;$(OutDir)\$(AssemblyName).obj" AdditionalOptions="/MACHINE:X64 /DLL /DEBUG /PDB:$(OutDir)\$(AssemblyName).pdb /DEF:format.def /OUT:$(OutDir)\$(AssemblyName).dll" />

if(WIN32)
    if(0)
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.mc
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
        ${SC_OUTPUT_DIR}/scerrres.rc
      COMMAND
        mc.exe -r ${SC_OUTPUT_DIR} -v ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        ${SC_OUTPUT_DIR}/scerrres.mc
      COMMENT
        "Generating scerrres.h"
    )
    add_custom_command(
      OUTPUT
        ${SC_OUTPUT_DIR}/scerrres.res
      COMMAND
        rc.exe scerrres.rc
      WORKING_DIRECTORY
        ${SC_OUTPUT_DIR}
      DEPENDS
        ${SC_OUTPUT_DIR}/scerrres.rc
      COMMENT
        "Generating scerrres.res"
    )
    endif()
    
    add_custom_target(Generate_SCERRRES_MC
      ALL
      COMMAND
        ${CMAKE_COMMAND} -E make_directory ${SC_OUTPUT_DIR}
      COMMAND
        $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ..\\errorcodes.xml -mc ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        scerrcc
      COMMENT
        "Generating scerrres.mc"
    )
    add_custom_target(Generate_SCERRRES_RC
      ALL
      COMMAND
        mc.exe -r ${SC_OUTPUT_DIR} -v ${SC_OUTPUT_DIR}/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}
      DEPENDS
        Generate_SCERRRES_MC
      COMMENT
        "Generating scerrres.h"
    )
    add_custom_target(Generate_SCERRRES_RES
      ALL
        COMMAND
            rc.exe scerrres.rc
          WORKING_DIRECTORY
            ${SC_OUTPUT_DIR}
          DEPENDS
            Generate_SCERRRES_RC
          COMMENT
            "Generating scerrres.res"
    )
else()
    message(SEND_ERROR "implement scerrres.h generation for Linux")
endif()

add_custom_target(Generate_SCERRRES_H
    DEPENDS
        scerrcc
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
        ${SC_OUTPUT_DIR}/scerrres.rc
        ${SC_OUTPUT_DIR}/scerrres.mc
)

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
#  ${SC_OUTPUT_DIR}/scerrres.res
#  ${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h
#  ${SC_OUTPUT_DIR}/scerrres.rc
#  ${SC_OUTPUT_DIR}/scerrres.mc
)
#set_source_files_properties(${CMAKE_CURRENT_SOURCE_DIR}/scerrres.h PROPERTIES GENERATED 1)
#set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.mc PROPERTIES GENERATED 1)
#set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.rc PROPERTIES GENERATED 1)
# set_source_files_properties(${SC_OUTPUT_DIR}/scerrres.res PROPERTIES GENERATED 1)
add_dependencies(scerrres scerrcc Generate_SCERRRES_RES)
# get_target_property(scerrres_LINKFLAGS scerrres LINK_FLAGS)
set(scerrres_LINKFLAGS "/DEBUG ${SC_OUTPUT_DIR}/scerrres.res")
set_target_properties(scerrres PROPERTIES LINK_FLAGS ${scerrres_LINKFLAGS})
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")

