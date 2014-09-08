# level1/src/Starcounter.ErrorCodes/ErrorCodeCompiler/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.8)

find_package(CSharp)
include(UseCSharp)

csharp_add_executable(scerrcc
    System.Web.dll
    ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/Starcounter.Errors.dll
    scerrcc/Program.cs
    scerrcc/CommandLine.cs
    scerrcc/Properties/AssemblyInfo.cs
)
add_dependencies(scerrcc Starcounter.Errors)
