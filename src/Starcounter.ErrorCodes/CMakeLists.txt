# level1/src/Starcounter.ErrorCodes/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

find_package(CSharp)
include(UseCSharp)

add_subdirectory(Starcounter.Errors)
add_subdirectory(ErrorCodeCompiler)
add_subdirectory(scerrres)

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
set_property(TARGET Generate_SCERRRES_H PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
