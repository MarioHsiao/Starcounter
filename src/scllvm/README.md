= Usage =

scllvm package consists of three files clang++ compiler, scllvm shared and static libraries.
scllvm binary files are not stored in GIT but pulled from FTP by "behemoth" project in Level0 (binaries are later copied to Level1 with other Level0 binaries).
When updated, the version number of scllvm increases, and dependencies should update the version number correspondinly (see CMakeLists.txt for behemoth project).

For examples, on how to use SCLLVM please look in "scllvm_tests" folder, for both managed and native examples.
On Windows scllvm saves cached modules in USER temp "starcounter" directory.
On Linux it saves modules in "/var/tmp/starcounter" (or in directory defined in "TMPDIR" environment variable like it does for TeamCity).
scllvm calls clang++.exe in order to produce object file that is cached. Cached module key is calculated as SHA-256 on input source code, however user can
supply a given hash key. User can also check if the module is already cached, delete the module, etc. Look at "scllvm_tests" for such examples.

scllvm supports adding custom parameters to clang++ and link external shared libraries (consult "scllvm_tests" for the example).
When no extra parameters are given the following command line is used:
"clang++ -Wall -O3 -c -mcmodel=large <path_to_gen_cpp_file> -o <path_to_obj_file>"
When extra parameters are supplied, the following command line is used (note that -O3 is omitted, so you have to supply it if needed):
"clang++ -c -mcmodel=large <your_parameters_here> <path_to_gen_cpp_file> -o <path_to_obj_file>"
("path_to_gen_cpp_file" and "path_to_obj_file" are provided by scllvm, so you can't affect them)

To enable diagnostics for scllvm (prints to console full clang++ command, notifies if module is not cached) set env var "SCLLVM_DIAG_ON" to true.
scllvm start its diagnostic messages using "[scllvm-<version>]" string, so you can find those, for example, in TeamCity build log.
For example:
[scllvm-2.2.1]: running clang tool: clang++ -c -mcmodel=large -O3 -std=c++11 "/home/teamcity/buildAgent/temp/buildTmp/starcounter/db-3740270811-56283-56283/sql/2.2.1/063f1b62675505a89098f9a4f42fada4b3e2b1b52a8180337efbbddc471001ed.cpp" -o "/home/teamcity/buildAgent/temp/buildTmp/starcounter/db-3740270811-56283-56283/sql/2.2.1/063f1b62675505a89098f9a4f42fada4b3e2b1b52a8180337efbbddc471001ed"


scllvm ALWAYS prints output from clang++ even warnings, since normally there should be no warnings/errors. In later versions we will treat warnings as errors for clang++.