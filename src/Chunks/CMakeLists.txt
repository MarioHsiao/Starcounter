# level1/src/Chunks/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_UNICODE -DUNICODE)

if(WIN32)
  add_subdirectory(server) # NEED_PORTING
  add_subdirectory(scipcmonitor) # NEED_PORTING
  # add_subdirectory(server_test) # MISSING: ../common/shared_memory_manager.hpp
  # add_subdirectory(BMX) # NEED_PORTING
  # add_subdirectory(test) # NEED_PORTING
endif()
