# level1/src/Chunks/server_test/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_executable(server_test main.cpp)
add_dependencies(server_test Generate_SCERRRES_H)
add_test(NAME server_test COMMAND $<TARGET_FILE:server_test>)
