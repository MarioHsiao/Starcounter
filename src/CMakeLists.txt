# level1/src/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_CRT_SECURE_NO_WARNINGS)

include_directories(Starcounter.ErrorCodes/scerrres)

add_subdirectory(Starcounter.ErrorCodes)
add_subdirectory(Chunks)
add_subdirectory(Hosting)
add_subdirectory(HTTP)
add_subdirectory(libs)
add_subdirectory(scnetworkgateway)
add_subdirectory(scservicelib)
add_subdirectory(Starcounter.Installer)
add_subdirectory(Utilities)

#
# autogenerate CMakeLists.txt from .csproj
#
macro(sc_process_all_csproj)
  file(GLOB_RECURSE CSPROJ_PATH_LIST RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} *.csproj)
  foreach(CSPROJ_PATH ${CSPROJ_PATH_LIST})
    get_filename_component(CSPROJ_DIR ${CSPROJ_PATH} DIRECTORY)
    get_filename_component(CSPROJ_FILENAME ${CSPROJ_PATH} NAME)
    string(REPLACE ".csproj" "" CSPROJ_NAME ${CSPROJ_FILENAME})
    string(REPLACE " " "_" CSPROJ_NAME ${CSPROJ_NAME})
    if(WIN32)
        include_external_msproject(${CSPROJ_NAME} "${CMAKE_CURRENT_SOURCE_DIR}/${CSPROJ_PATH}" TYPE FAE04EC0-301F-11D3-BF4B-00C04F79EFBC)
        set_property(TARGET ${CSPROJ_NAME} PROPERTY FOLDER "level1")
    else()
        message(SEND_WARNING "TODO: CSPROJ_PATH ${CSPROJ_PATH}")
        add_custom_command(
            OUTPUT
                "${CSPROJ_DIR}/${CSPROJ_NAME}.cmake"
            COMMAND
                $<TARGET_FILE:csproj2cmake> "${CSPROJ_PATH}" "${CSPROJ_NAME}.cmake"
            WORKING_DIRECTORY
                ${CMAKE_CURRENT_SOURCE_DIR}
            DEPENDS
                "${CSPROJ_PATH}"
                csproj2cmake
            COMMENT
                "csproj2cmake ${CSPROJ_NAME}"
        )
        add_custom_target(CSPROJ2CMAKE_${CSPROJ_NAME} ALL DEPENDS "${CSPROJ_DIR}/${CSPROJ_NAME}.cmake")
        set_property(TARGET CSPROJ2CMAKE_${CSPROJ_NAME} PROPERTY FOLDER "level1/csproj2cmake/${CSPROJ_DIR}")
    endif()
  endforeach()
endmacro()

# sc_process_all_csproj()
