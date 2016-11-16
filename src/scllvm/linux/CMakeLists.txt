cmake_minimum_required (VERSION 2.8)

project(scllvm)

set(SCLLVMTarget scllvm)

# Path to unzipped LLVM sources.
set(LLVMClangSrcDir ~/Documents/llvm-3.9.0.src)

# Path to LLVM output directory.
set(LLVMClangOutDir ${LLVMClangSrcDir}/build)

# To get needed cxx flags, call "./llvm-config --cxxflags" from "LLVMClangOutDir/bin"
# Here we have release LLVM, that gave us the following release options.
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -fvisibility-inlines-hidden -Wall -W -Wno-unused-parameter -Wwrite-strings -Wcast-qual -Wno-missing-field-initializers -pedantic -Wno-long-long -Wno-maybe-uninitialized -Wdelete-non-virtual-dtor -Wno-comment -std=c++11 -ffunction-sections -fdata-sections -O3 -DNDEBUG  -fno-exceptions -fno-rtti -D_GNU_SOURCE -D__STDC_CONSTANT_MACROS -D__STDC_FORMAT_MACROS -D__STDC_LIMIT_MACROS")

include_directories(${LLVMClangSrcDir}/tools/clang/include ${LLVMClangOutDir}/tools/clang/tools/clang-check ${LLVMClangSrcDir}/tools/clang/tools/clang-check ${LLVMClangOutDir}/tools/clang/include ${LLVMClangOutDir}/include ${LLVMClangSrcDir}/include)

link_directories(${LLVMClangOutDir} ${LLVMClangOutDir}/lib)

add_library(${SCLLVMTarget} SHARED scllvm.cpp)
#add_library(${SCLLVMTarget} STATIC scllvm.cpp)

# Uncomment the following to create executable and run it so the tests are invoked.
#add_executable(${SCLLVMTarget} scllvm.cpp)

# To get a list of LLVM libs, call "./llvm-config --libs" from "LLVMClangOutDir/bin"

target_link_libraries(${SCLLVMTarget} clangFrontend clangDriver clangSerialization clangCodeGen clangParse clangSema clangAnalysis clangEdit clangAST clangLex clangBasic LLVMLTO LLVMObjCARCOpts LLVMSymbolize LLVMDebugInfoPDB LLVMDebugInfoDWARF LLVMMIRParser LLVMLibDriver LLVMOption gtest_main gtest LLVMTableGen LLVMOrcJIT LLVMPasses LLVMipo LLVMVectorize LLVMLinker LLVMIRReader LLVMAsmParser LLVMX86Disassembler LLVMX86AsmParser LLVMX86CodeGen LLVMSelectionDAG LLVMAsmPrinter LLVMX86Desc LLVMMCDisassembler LLVMX86Info LLVMX86AsmPrinter LLVMX86Utils LLVMMCJIT LLVMLineEditor LLVMDebugInfoCodeView LLVMInterpreter LLVMExecutionEngine LLVMRuntimeDyld LLVMCodeGen LLVMTarget LLVMScalarOpts LLVMInstCombine LLVMInstrumentation LLVMProfileData LLVMObject LLVMMCParser LLVMTransformUtils LLVMMC LLVMBitWriter LLVMBitReader LLVMAnalysis LLVMCore LLVMSupport -lrt -ldl -lpthread -lm)