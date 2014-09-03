# level1/src/Starcounter.ErrorCodes/CMakeLists.txt


cmake_minimum_required(VERSION 2.8)

find_package(CSharp)
include(UseCSharp)

set(CSHARP_SOURCE_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR})
set(CSHARP_BINARY_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR})
csharp_add_executable(cserrcc
    Program.cs
    CommandLine.cs
    Properties/AssemblyInfo.cs
)

if(0)

set(COLLATION_DIR ${CMAKE_CURRENT_SOURCE_DIR}/Collations)

set(TURBOTEXTGENERATOR_SOURCES
  TurboTextGenerator.cs
  BitSegment.cs
  CppGenerator.cs
  HeaderGenerator.cs
  TurboTextHelper.cs
  XmlNormalizer.cs
)

if(WIN32)
  set(TURBOTEXTGENERATOR_EXE ${CMAKE_CURRENT_BINARY_DIR}\\TurboTextGenerator.exe)
  set(COMPILE_TURBOTEXT_GENERATOR csc -out:${TURBOTEXTGENERATOR_EXE})
  set(CSHARP_RUNTIME)
elseif(APPLE)
  set(TURBOTEXTGENERATOR_EXE ${CMAKE_CURRENT_BINARY_DIR}/TurboTextGenerator.exe)
  set(COMPILE_TURBOTEXT_GENERATOR mcs -debug -out:${TURBOTEXTGENERATOR_EXE})
  set(CSHARP_RUNTIME mono --debug)
else()
  set(TURBOTEXTGENERATOR_EXE ${CMAKE_CURRENT_BINARY_DIR}/TurboTextGenerator.exe)
  set(COMPILE_TURBOTEXT_GENERATOR mcs -debug -out:${TURBOTEXTGENERATOR_EXE})
  set(CSHARP_RUNTIME mono --debug)
endif()

set(TURBOTEXTGENERATOR_EXE ${TURBOTEXTGENERATOR_EXE} PARENT_SCOPE)
set(CSHARP_RUNTIME ${CSHARP_RUNTIME} PARENT_SCOPE)

add_custom_command(
  OUTPUT
    ${TURBOTEXTGENERATOR_EXE}
  COMMAND
    ${COMPILE_TURBOTEXT_GENERATOR} ${TURBOTEXTGENERATOR_SOURCES}
  WORKING_DIRECTORY
    ${CMAKE_CURRENT_SOURCE_DIR}
)

add_custom_target(TURBOTEXT_GENERATOR
  DEPENDS
    ${TURBOTEXTGENERATOR_EXE}
  SOURCES
    ${TURBOTEXTGENERATOR_SOURCES}
)
endif()
