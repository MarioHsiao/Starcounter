# level1/src/Chunks/scipcmonitor/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(scipcmonitor)

set(scipcmonitor_SOURCE_FILES
    ../common/mapped_region.cpp
    ../common/shared_memory_object.cpp
    monitor.cpp
)

set(scipcmonitor_HEADER_FILES
    event.hpp
    monitor.hpp
)

add_executable(scipcmonitor ${scipcmonitor_SOURCE_FILES} ${scipcmonitor_HEADER_FILES})
target_link_libraries(scipcmonitor
    scerrres
)
