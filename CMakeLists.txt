# level1/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)
project(level1)
enable_testing()

function(uod_set_starcounter_level1)
  if(CMAKE_BUILD_TYPE)
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin/${CMAKE_BUILD_TYPE})
  elseif(CMAKE_CFG_INTDIR)
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin/${CMAKE_CFG_INTDIR})
  else()
    message(SEND_ERROR "Can't figure out output directories")
    set(UOD_ROOT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin)
  endif()
  
  set(UOD_ROOT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${UOD_ROOT_DIRECTORY} PARENT_SCOPE)
  if(CMAKE_CONFIGURATION_TYPES)
    foreach(uod_cfg ${CMAKE_CONFIGURATION_TYPES})
      string(TOUPPER ${uod_cfg} uod_cfg_uc)
      set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/bin/${uod_cfg} PARENT_SCOPE)
      set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/bin/${uod_cfg} PARENT_SCOPE)
      set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${uod_cfg_uc} ${CMAKE_CURRENT_SOURCE_DIR}/bin/${uod_cfg} PARENT_SCOPE)
    endforeach()
  endif()
endfunction()

uod_set_starcounter_level1()
link_directories(${CMAKE_ARCHIVE_OUTPUT_DIRECTORY})

find_package(CSharp REQUIRED)

add_subdirectory(src)
# add_subdirectory(StarcounterJs)
