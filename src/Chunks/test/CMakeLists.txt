# level1/src/Chunks/test/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

project(sc_ipc_test)

include_directories(../../../../level0/src/include)

set(sc_ipc_test_SOURCE_FILES
    ../common/mapped_region.cpp
    ../common/shared_memory_object.cpp
    test.cpp
    tiny_tuple/benchmark.cpp
    tiny_tuple/record_data.cpp
)

set(sc_ipc_test_HEADER_FILES
    decimal/decimal.hpp
    decimal/impl/decimal.hpp
    decimal/impl/uint128_t.hpp
    decimal/macro_definitions.hpp
    decimal/uint128_t.hpp
    test.hpp
    impl/test.hpp
    impl/worker.hpp
    random.hpp
    tiny_tuple/bit_operations.hpp
    tiny_tuple/data_header.hpp
    tiny_tuple/defined_column_value.hpp
    tiny_tuple/impl/data_header.hpp
    tiny_tuple/impl/defined_column_value.hpp
    tiny_tuple/impl/random.hpp
    tiny_tuple/impl/record_header.hpp
    tiny_tuple/impl/test.hpp
    tiny_tuple/impl/tiny_tuple.hpp
    tiny_tuple/macro_definitions.hpp
    tiny_tuple/random.hpp
    tiny_tuple/record_data.hpp
    tiny_tuple/record_header.hpp
    tiny_tuple/test.hpp
    tiny_tuple/tiny_tuple.hpp
    worker.hpp
)

add_executable(sc_ipc_test ${sc_ipc_test_SOURCE_FILES} ${sc_ipc_test_HEADER_FILES})
add_dependencies(sc_ipc_test Generate_SCERRRES_H)
OrganizeVisualStudio(sc_ipc_test) # "level1/Chunks"
target_link_libraries(sc_ipc_test
    bmx
)
add_test(NAME sc_ipc_test COMMAND $<TARGET_FILE:sc_ipc_test>)
