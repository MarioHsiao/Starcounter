#include <cstdint>
#include <assert.h>
#include <windows.h>
#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>
#include <direct.h>
#include <stdio.h>
#include <stdlib.h>
#include <vector>

//#define USE_DLLS_DIRECTLY

#ifdef USE_DLLS_DIRECTLY
#pragma comment(lib, "../x64/Release/external_dll1.lib")
#pragma comment(lib, "../x64/Release/external_dll2.lib")
extern "C" int32_t external_dll1_func();
extern "C" int32_t external_dll2_func();
int32_t FuncSimulation(int32_t p) {
	return external_dll1_func() + external_dll2_func() + p;
}
#endif

extern "C" uint32_t ClangCompileAndLoadObjectFile(
	void** const clang_engine,
	const bool print_to_console,
	const bool do_optimizations,
	const wchar_t* const path_to_cache_dir,
	const char* const predefined_hash_str,
	const char* const input_code_str,
	const char* const function_names_delimited,
	const char* const ext_libraries_names_delimited,
	const bool delete_sources,
	void* out_func_ptrs[],
	void** out_exec_engine);

extern "C" void ClangInit();

extern "C" void ClangDeleteModule(void* const clang_engine, void** exec_module);

// TODO: Fix for Linux.
std::wstring GetTempDirForTests() {

	wchar_t temp_dir_path[1024];
	uint32_t num_chars = GetTempPath(1024, temp_dir_path);
	assert(num_chars > 0);

	std::wstring test_temp_dir = temp_dir_path;
	test_temp_dir += L"starcounter";

	return test_temp_dir;
}

// Replaces string in string.
std::string ReplaceString(std::string subject, const std::string& search,
	const std::string& replace) {
	size_t pos = 0;
	while ((pos = subject.find(search, pos)) != std::string::npos) {
		subject.replace(pos, search.length(), replace);
		pos += replace.length();
	}
	return subject;
}

void TestMemoryUsage() {

	ClangInit();

	void* cge = NULL;

	std::ifstream ifs("generated1.cpp");
	std::string orig_code_string((std::istreambuf_iterator<char>(ifs)), (std::istreambuf_iterator<char>()));

	std::vector<void*> engines;

	const std::wstring test_temp_dir = GetTempDirForTests();

	std::vector<void*> out_func_ptrs;

	const int32_t kNumCodegenFunctions = 1000;
	for (int32_t i = 0; i < kNumCodegenFunctions; i++) {

		std::string code_string = ReplaceString(orig_code_string, "12345", std::to_string(i));

		const char * const code = code_string.c_str();
		void* exec_engine;
		void* out_func_ptr;

		uint32_t err = ClangCompileAndLoadObjectFile(
			&cge, true, true, test_temp_dir.c_str(), NULL,
			code, "gen_function", "external_dll1.dll;external_dll2.dll",
			false, &out_func_ptr, &exec_engine);

		assert((!err) && "ClangCompileCodeAndGetFuntions returned non-zero exit code!");

		engines.push_back(exec_engine);

#ifdef USE_DLLS_DIRECTLY
		out_func_ptrs.push_back(FuncSimulation);
#else
		out_func_ptrs.push_back(out_func_ptr);
#endif
	}

	clock_t start_time = clock();

	typedef int(*function_type) (int);

	int32_t k = 0;
	const int32_t kNumCallsEachFunction = 100000;
	for (auto &out_func_ptr : out_func_ptrs) {

		for (int32_t i = 0; i < kNumCallsEachFunction; i++) {

			int func_result = (function_type(out_func_ptr))(i);
			if (134786 + 265688 + k + i != func_result) {
				assert(!"Generated increment function returned wrong result!");
			}
		}
		
		if (k % 10 == 0)
			std::cout << "Called: " << k << std::endl;

		k++;
	}

	clock_t end_time = clock();

	float seconds_time = (float)(end_time - start_time) / CLOCKS_PER_SEC;

    std::cout << "Calling " << kNumCodegenFunctions << " functions " << kNumCallsEachFunction << " times took seconds: " << seconds_time << std::endl;

	// Deleting modules.
	for (auto &e : engines) {
		ClangDeleteModule(cge, &e);
	}

	std::cout << "Simple code generation test passed!" << std::endl;
}

int32_t main() {

	TestMemoryUsage();

	return 0;
}
