# level1/src/Hosting/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

include_directories(../../../level0/src/include)
add_definitions(-D_UNICODE -DUNICODE)

add_subdirectory(sccoreapp)
add_subdirectory(sccoredbh)

# Starcounter.Hosting/Starcounter.Hosting.csproj
