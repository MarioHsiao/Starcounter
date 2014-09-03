# level1/src/Starcounter.ErrorCodes/Starcounter.Errors/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

find_package(CSharp)
include(UseCSharp)

set(CSHARP_SOURCE_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR})
set(CSHARP_BINARY_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
csharp_add_library(Starcounter.Errors
    ErrorCode.cs
    ErrorFile.cs
    ErrorFileReader.cs
    Facility.cs
    Severity.cs
)

if(0)
csharp_add_executable(cserrcc
    ErrorCodeCompiler/scerrcc/Program.cs
    ErrorCodeCompiler/scerrcc/CommandLine.cs
    ErrorCodeCompiler/scerrcc/Properties/AssemblyInfo.cs
    Starcounter.Errors/ErrorCode.cs
    Starcounter.Errors/ErrorFile.cs
    Starcounter.Errors/ErrorFileReader.cs
    Starcounter.Errors/Facility.cs
    Starcounter.Errors/Severity.cs
)
endif()
# add_subdirectory(ErrorCodeCompiler/scerrcc)
