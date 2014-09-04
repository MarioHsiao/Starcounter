# level1/src/Starcounter.ErrorCodes/Starcounter.Errors/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

find_package(CSharp)
include(UseCSharp)

csharp_add_library(Starcounter.Errors
    ErrorCode.cs
    ErrorFile.cs
    ErrorFileReader.cs
    Facility.cs
    Severity.cs
)
