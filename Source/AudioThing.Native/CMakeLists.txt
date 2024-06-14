cmake_minimum_required(VERSION 3.16)

set(CMAKE_CXX_STANDARD "20")

project(AudioThing.Native CXX)

find_package(ALSA REQUIRED)

file(GLOB_RECURSE SOURCES CONFIGURE_DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/Source/*)
add_library(AudioThing.Native SHARED ${SOURCES})
target_link_libraries(AudioThing.Native PRIVATE ${ALSA_LIBRARIES})
