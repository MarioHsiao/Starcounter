# level1/src/Starcounter.ErrorCodes/ErrorCodeCompiler/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.8)

find_package(CSharp)
include(UseCSharp)

get_property(starcounter_errors_dll TARGET Starcounter.Errors PROPERTY OUTPUT_NAME)

csharp_add_executable(scerrcc
    System.Web.dll
    ${starcounter_errors_dll}
    scerrcc/Program.cs
    scerrcc/CommandLine.cs
    scerrcc/Properties/AssemblyInfo.cs
)
# ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/Starcounter.Errors.dll
add_dependencies(scerrcc Starcounter.Errors)
set_property(TARGET scerrcc PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
