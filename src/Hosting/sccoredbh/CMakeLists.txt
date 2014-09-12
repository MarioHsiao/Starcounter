# level1/src/Hosting/sccoredbh/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(sccoredbh)

set(sccoredbh_SOURCE_FILES
    sccoredbh.cpp
    sccoredbh.def
)

set(sccoredbh_HEADER_FILES
    external.h
    internal.h
)
add_library(sccoredbh SHARED ${sccoredbh_SOURCE_FILES} ${sccoredbh_HEADER_FILES})
set_property(TARGET sccoredbh PROPERTY FOLDER "level1")
target_link_libraries(sccoredbh
    coalmine
    sccoredb
    sccorelib
    sccorelog
    sccoreapp
    sunflower
)
