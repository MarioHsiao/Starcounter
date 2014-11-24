# level1/src/Starcounter.ErrorCodes/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

if(0)
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_CURRENT_SOURCE_DIR}/cmake")
set_property(GLOBAL PROPERTY USE_FOLDERS ON)
function(uod_set_starcounter)
  if(WIN32)
    set(UOD_PLATFORM msbuild)
  else()
    set(UOD_PLATFORM make)
  endif()
  if(CMAKE_BUILD_TYPE)
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64/${CMAKE_BUILD_TYPE})
  elseif(CMAKE_CFG_INTDIR)
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64/${CMAKE_CFG_INTDIR})
  else()
    message(SEND_ERROR "Can't figure out output directories")
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64)
  endif()
  
  set(UOD_ROOT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  if(CMAKE_CONFIGURATION_TYPES)
    foreach(uod_cfg ${CMAKE_CONFIGURATION_TYPES})
      string(TOUPPER ${uod_cfg} uod_cfg_uc)
      set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64/${uod_cfg} PARENT_SCOPE)
      set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64/${uod_cfg} PARENT_SCOPE)
      set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/${UOD_PLATFORM}/x64/${uod_cfg} PARENT_SCOPE)
    endforeach()
  endif()
endfunction()
uod_set_starcounter()
link_directories(${CMAKE_ARCHIVE_OUTPUT_DIRECTORY})
endif()

find_package(CSharp)
include(UseCSharp)

add_subdirectory(Starcounter.Errors)
add_subdirectory(ErrorCodeCompiler)
add_subdirectory(scerrres)

if(0)
add_custom_target(CompileXMLErrorsToCSharp
  COMMAND
    ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ${CMAKE_CURRENT_SOURCE_DIR}/errorcodes.xml -cs
  DEPENDS
    scerrcc
    errorcodes.xml
  WORKING_DIRECTORY
    ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
  COMMENT
    "Trigger target CompileXMLErrorsToCSharp"
)
set_property(TARGET CompileXMLErrorsToCSharp PROPERTY FOLDER "level1/Starcounter.ErrorCodes")

if(WIN32)
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_BINARY_DIR}/scerrres/scerrres.mc
      COMMAND
        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ${CMAKE_CURRENT_SOURCE_DIR}/errorcodes.xml -mc ${CMAKE_CURRENT_BINARY_DIR}/scerrres/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres
      DEPENDS
        scerrcc
      COMMENT
        "Running scerrcc.exe"
    )
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres/scerrres.h
      COMMAND
        mc.exe -r ${CMAKE_CURRENT_SOURCE_DIR}/scerrres -v ${CMAKE_CURRENT_BINARY_DIR}/scerrres/scerrres.mc
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres
      DEPENDS
        ${CMAKE_CURRENT_BINARY_DIR}/scerrres/scerrres.mc
      COMMENT
        "Running mc.exe"
    )
else()
    add_custom_command(
      OUTPUT
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres/scerrres.h
      COMMAND
        ${CSHARP_INTERPRETER} $<TARGET_PROPERTY:scerrcc,OUTPUT_NAME> ${CMAKE_CURRENT_SOURCE_DIR}/errorcodes.xml -mc ${CMAKE_CURRENT_BINARY_DIR}/scerrres/scerrres.mc -ch ${CMAKE_CURRENT_SOURCE_DIR}/scerrres/scerrres.h
      WORKING_DIRECTORY
        ${CMAKE_CURRENT_SOURCE_DIR}/scerrres
      DEPENDS
        scerrcc
    )
    # ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/scerrcc.exe 
endif()

add_custom_target(Generate_SCERRRES_H
  DEPENDS
    ${CMAKE_CURRENT_SOURCE_DIR}/scerrres/scerrres.h
  COMMENT
    "Trigger target Generate_SCERRRES_H"
)
endif()

