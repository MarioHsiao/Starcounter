# level1/StarcounterJS/StarcounterJS/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(StarcounterJS)

set(StarcounterJS_SOURCE_FILES
    bootstrap-starcounter.cc
    main.cc
    bootstrap.cc
    memory.cc
    interactive-shell.cc
    bootstrap-v8.cc
)

set(StarcounterJS_HEADER_FILES
    starcounter-host.h
    v8-host.h
)

add_executable(StarcounterJS ${StarcounterJS_SOURCE_FILES} ${StarcounterJS_HEADER_FILES})
target_link_libraries(StarcounterJS
    coalmine
    formaterr
    sccoredb
#    sccorelib
    sccorelog
    sunflower
    v8_base
    v8_snapshot
)
