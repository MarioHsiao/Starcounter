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

VirtualBox:
In Menu: Devices->Insert Guest Additions CD Image...
or through commands:
* sudo apt-get install virtualbox-guest-dkms
* sudo apt-get install virtualbox-guest-x11


* Use latest Ubuntu (in my case 16 LTS)
* Unzip llvm sources to some dir [llvm], in this case "~/Documents/llvm-3.9.0.src"
* Unzip clang sources to [llvm]\tools\clang\
* Not necessary but in case: "sudo apt-get build-dep llvm"
* sudo apt-get install cmake
* Create a `build` directory inside "~/Documents/llvm-3.9.0.src"
* Inside `build` dir: cmake -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Release  -DLLVM_TARGETS_TO_BUILD="X86" "~/Documents/llvm-3.9.0.src"
* Go to this `build` directory and call `make`
* To get info from LLVM to insert in CMakeLists.txt:
              llvm-config --cxxflags
              llvm-config --ldflags
              llvm-config --libs
* To share the folder with Ubuntu in VirtualBox: goto "Settings->Shared folders" select the folder and check "Auto-mount".
Then start the Ubuntu machine and type "sudo adduser [currentusername] vboxsf"
* Go to scllvm project directory and create a new folder "build":
      * cd build
      * cmake ..
      * make
	  * The output will be either "libscllvm.so" or "scllvm" in "build" directory, depending on type.

