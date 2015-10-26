Useful links: http://llvm.org/docs/CMake.html

= Building LLVM =

*Windows*

In VS 2015:


1. Unzip llvm sources to some dir [llvm].
2. Unzip clang sources to [llvm]\tools\clang\
3. In cmake gui set output dir to: [llvm]\VS
4. Set string variables in cmake gui to statically link VS2015 CRT: LLVM_USE_CRT_DEBUG MTd
LLVM_USE_CRT_RELEASE MT



*Linux*

* Use latest Ubuntu (in my case 15)
* sudo apt-get install virtualbox-guest-dkms
* sudo apt-get install virtualbox-guest-x11
* Unzip llvm sources to some dir [llvm], in this case "~/Documents/llvm-3.7.0.src"
* Unzip clang sources to [llvm]\tools\clang\
* sudo apt-get build-dep llvm
* sudo apt-get install cmake
* cmake -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Release  -DLLVM_TARGETS_TO_BUILD="X86" "~/Documents/llvm-3.7.0.src"
* To get info from LLVM to insert in CMakeLists.txt:
              llvm-config --cxxflags
              llvm-config --ldflags
              llvm-config --libs
* Go to scllvm project directory and create a new folder "build":
      * cd build
      * cmake ..
      * make
      * ./scllvm (to run tests and see if everything is working fine)

