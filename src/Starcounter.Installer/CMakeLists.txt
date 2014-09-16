# level1/src/Starcounter.Installer/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_UNICODE -DUNICODE)

add_subdirectory(Starcounter.InstallerNativeHelper)
add_subdirectory(Starcounter.InstallerNativeWrapper)
