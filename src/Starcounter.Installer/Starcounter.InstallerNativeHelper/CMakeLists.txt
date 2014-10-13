# level1/src/Starcounter.Installer/Starcounter.InstallerNativeHelper/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_library(InstallerNativeHelper SHARED
    InstallerNativeHelper.cpp
    cpuid.hpp
    InstallerNativeHelper.def
)
set_property(TARGET InstallerNativeHelper PROPERTY FOLDER "level1/Starcounter.Installer")
