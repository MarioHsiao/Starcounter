# level1/src/Utilities/CreateShortcut/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_UNICODE -DUNICODE)

add_library(CreateShortcut SHARED
    CreateShortcut.cpp
)
set_property(TARGET CreateShortcut PROPERTY FOLDER "level1/Utilities")
