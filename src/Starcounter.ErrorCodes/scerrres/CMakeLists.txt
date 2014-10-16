# level1/src/Starcounter.ErrorCodes/scerrres/CMakeLists.txt

cmake_minimum_required(VERSION 2.8)

add_library(scerrres SHARED
  Format.cpp
  Format.def
  format.h
  ../errorcodes.xml
)
set_property(TARGET scerrres PROPERTY FOLDER "level1/Starcounter.ErrorCodes")
