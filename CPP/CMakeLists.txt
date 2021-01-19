cmake_minimum_required (VERSION 3.6)

project ("PasteBot")

add_subdirectory(thirdparty/DisCPP)

add_executable (main "src/main.cpp")

set_target_properties(main PROPERTIES CXX_STANDARD 17 CXX_EXTENSIONS OFF)
target_include_directories(main PUBLIC include)

file(GLOB_RECURSE source_list src/*.cpp)
target_sources(main PRIVATE ${source_list})

target_link_libraries(main PRIVATE discpp)