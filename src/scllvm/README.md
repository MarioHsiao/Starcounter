= Usage =

scllvm package consists of three files clang++ compiler, scllvm shared and static libraries.
scllvm binary files are not stored in GIT but pulled from FTP by "behemoth" project in Level0 (binaries are later copied to Level1 with other Level0 binaries).
When updated, the version number of scllvm increases, and dependencies should update the version number correspondinly (see CMakeLists.txt for behemoth project).

For examples, on how to use SCLLVM please look in "scllvm_tests" folder, for both managed and native examples.
On Windows scllvm saves cached modules in USER temp "starcounter" directory.
On Linux it saves modules in "/var/tmp/starcounter" (or in directory defined in "TMPDIR" environment variable like it does for TeamCity).
scllvm calls clang++.exe in order to produce object file that is cached. Cached module key is calculated as SHA-256 on input source code, however user can
supply a given hash key. User can also check if the module is already cached, delete the module, etc. Look at "scllvm_tests" for such examples.

To enable diagnostics for scllvm (prints to console full clang++ command, notifies if module is not cached) set env var "SCLLVM_DIAG_ON" to true.
scllvm ALWAYS prints output from clang++ even warnings, since normally there should be no warnings/errors. In later versions we will treat warnings as errors for clang++.