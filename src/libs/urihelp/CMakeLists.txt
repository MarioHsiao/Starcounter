# level1/src/libs/urihelp/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_library(urihelp STATIC
    urihelp.cpp
)
OrganizeVisualStudio(urihelp) # "level1/libs"

# libs\urihelp\urihelp.vcxproj
