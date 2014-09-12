# level1/src/Chunks/server/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

PROJECT(server)

# find_package(Boost)
# include_directories(${Boost_INCLUDE_DIRS})
# link_directories(${Boost_LIBRARY_DIRS})

SET(server_SOURCE_FILES
    ../common/mapped_region.cpp
    ../common/shared_memory_object.cpp
    initialize.cpp
    scheduler.cpp
    server.cpp
    server_test.cpp
    server.def
)

SET(server_HEADER_FILES
    # ../../common/impl/database_number.hpp # doesn't exist
    # ../common/database_number.hpp # doesn't exist
    ../common/atomic_bounded_buffer.hpp
    ../common/atomic_buffer.hpp
    ../common/bounded_buffer.hpp
    ../common/channel.hpp
    ../common/channel_mask.hpp
    ../common/channel_number.hpp
    ../common/chunk.hpp
    ../common/circular_buffer.hpp
    ../common/client_interface.hpp
    ../common/client_number.hpp
    ../common/common_scheduler_interface.hpp
    ../common/config_param.hpp
    ../common/interprocess.hpp
    ../common/macro_definitions.hpp
    ../common/monitor_interface.hpp
    ../common/name_definitions.hpp
    ../common/owner_id.hpp
    ../common/scheduler_channel.hpp
    ../common/scheduler_interface.hpp
    ../common/scheduler_mask.hpp
    ../common/scheduler_number.hpp
    ../common/shared_interface.hpp
    initialize.hpp
    scheduler.hpp
    server.hpp
)

SET_SOURCE_FILES_PROPERTIES(server_HEADER_FILES PROPERTIES HEADER_FILE_ONLY TRUE)
LIST(APPEND server_SOURCE_FILES ${server_HEADER_FILES})

ADD_LIBRARY(server SHARED ${server_SOURCE_FILES})
