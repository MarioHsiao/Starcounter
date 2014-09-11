# level1/src/Starcounter.ErrorCodes/ErrorCodeCompiler/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

find_package(CSharp)
include(UseCSharp)

csharp_add_executable(cserrcc
    Program.cs
    CommandLine.cs
    Properties/AssemblyInfo.cs
)
