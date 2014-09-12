# level1/src/Chunks/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_UNICODE -DUNICODE)

add_subdirectory(server)
# add_subdirectory(server_test) # MISSING: ../common/shared_memory_manager.hpp
add_subdirectory(BMX)
add_subdirectory(scipcmonitor)
add_subdirectory(test)
