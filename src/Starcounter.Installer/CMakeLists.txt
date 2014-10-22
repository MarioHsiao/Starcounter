# level1/src/Starcounter.Installer/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_UNICODE -DUNICODE)

add_subdirectory(Starcounter.InstallerNativeHelper)

# TODO
# MISSING InstallerNativeWrapper/resources/dotnetfx45_full_x86_x64.exe
# MISSING InstallerNativeWrapper/resources/Starcounter-Setup.exe
# add_subdirectory(Starcounter.InstallerNativeWrapper)
