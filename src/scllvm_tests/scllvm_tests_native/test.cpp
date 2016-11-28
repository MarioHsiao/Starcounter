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
#include <array>
#include <gtest/gtest.h>

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

extern "C" uint32_t ScLLVMProduceModule(
#ifdef _WIN32
    const wchar_t* const path_to_cache_dir,
    const wchar_t* const cache_sub_dir,
#else
    const char* const path_to_cache_dir,
    const char* const cache_sub_dir,
#endif
	const char* const predefined_hash_str,
	const char* const code_to_build,
	const char* const function_names_delimited,
	const char* const ext_libraries_names_delimited,
	const bool delete_sources,
	const char* const predefined_clang_params,
	char* const out_hash_65bytes,
	float* const out_time_seconds,
	void* out_func_ptrs[],
	void** out_exec_module,
	void** const out_codegen_engine);

extern "C" void ScLLVMInit();

extern "C" bool ScLLVMIsModuleCached(
	const wchar_t* const path_to_cache_dir,
	const char* const predefined_hash_str);

extern "C" void ScLLVMDeleteModule(
	void* const clang_engine, void** exec_module);

extern "C" bool ScLLVMDeleteCachedModule(
	const wchar_t* const path_to_cache_dir,
	const char* const predefined_hash_str);

extern "C" void ScLLVMDestroy(void* codegen_engine);

extern "C" uint32_t ScLLVMCalculateHash(
	const char* const code_to_build,
	char* const out_hash_65bytes
);

void GetTempDirForTests(std::wstring& out_temp_dir) {

	wchar_t temp_dir_path[1024];
	uint32_t num_chars = GetTempPath(1024, temp_dir_path);
	ASSERT_TRUE(num_chars > 0) << "Could not get temporary path by some reason!";

	std::wstring test_temp_dir = temp_dir_path;
	test_temp_dir += L"starcounter";

	out_temp_dir = test_temp_dir;
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

TEST(ScLLVMTests, HashingTests) {
	char hash_str[65];

	uint32_t err_code = ScLLVMCalculateHash("", hash_str);
	ASSERT_EQ(1, err_code);

	err_code = ScLLVMCalculateHash("!", nullptr);
	ASSERT_EQ(0, err_code);

	err_code = ScLLVMCalculateHash("!", hash_str);
	ASSERT_EQ(0, err_code);
	ASSERT_STREQ("bb7208bc9b5d7c04f1236a82a0093a5e33f40423d5ba8d4266f7092c3ba43b62", hash_str);

	err_code = ScLLVMCalculateHash("extern \"C\" int Func1(int x) { return 8459649 + x; }", hash_str);
	ASSERT_EQ(0, err_code);
	ASSERT_STREQ("dfe742d590544942a0f01e28eb2dc52ae76d94cecb306a8e7ab98eed54949a46", hash_str);

	err_code = ScLLVMCalculateHash("Hello world!", hash_str);
	ASSERT_EQ(0, err_code);
	ASSERT_STREQ("c0535e4be2b79ffd93291305436bf889314e4a3faec05ecffcbb7df31ad9e51a", hash_str);

	err_code = ScLLVMCalculateHash(
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		"Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!Hello world!"
		, hash_str);

	ASSERT_EQ(0, err_code);
	ASSERT_STREQ("796b8367313e5286a2f01f2cacdcf1cfddbca4ecac6ddfb72671478da4bc7c67", hash_str);
}

TEST(ScLLVMTests, SimpleCachingCases) {

	void* cge = NULL;

	wchar_t cur_dir[1024];
	wchar_t* f = _wgetcwd(cur_dir, 1024);
	ASSERT_NE(f, nullptr) << "Can't get current working directory!";

	// Testing some non-existing cached code.
	bool found_in_cache = ScLLVMIsModuleCached(cur_dir, "234253453456456");
	ASSERT_FALSE(found_in_cache) << "Not existing cached module is found!";

	void* out_exec_module = NULL;
	void* out_func_ptr = NULL;

	uint32_t err;
	typedef int(*function_type) (int);

	// Building the simplest LLVM module and checking correctness.
	float time_took_sec_no_cache = 0;
	char out_hash_str[65];

	err = ScLLVMProduceModule(
		cur_dir,
        nullptr,
		nullptr,
		"extern \"C\" int Func1(int x) { return 8459649 + x; }",
		"Func1",
		nullptr,
		true,
		nullptr, 
		out_hash_str,
		&time_took_sec_no_cache,
		&out_func_ptr,
		&out_exec_module,
		&cge);

	ASSERT_EQ(err, 0) << "Could not build simplest LLVM module!";
	ASSERT_NE(out_exec_module, nullptr) << "Execution module is null!";
	ASSERT_NE(out_func_ptr, nullptr) << "No function pointer is returned from produced LLVM module!";
	
	int result = ((function_type)out_func_ptr)(123);
	ASSERT_EQ(result, 8459772) << "Wrong result returned from built function!";

	float time_took_sec_cached = 0;
	err = ScLLVMProduceModule(
		cur_dir,
        nullptr,
		nullptr,
		"extern \"C\" int Func1(int x) { return 8459649 + x; }",
		"Func1", 
		nullptr,
		true, 
		nullptr, 
		out_hash_str,
		&time_took_sec_cached,
		&out_func_ptr,
		&out_exec_module,
		&cge);

	ASSERT_EQ(err, 0) << "Could not build simplest LLVM module!";
	ASSERT_NE(out_exec_module, nullptr) << "Execution module is null!";
	ASSERT_NE(out_func_ptr, nullptr) << "No function pointer is returned from produced LLVM module!";

	result = ((function_type)out_func_ptr)(123);
	ASSERT_EQ(result, 8459772) << "Wrong result returned from built function!";

	// Checking that cached time is less.
	ASSERT_LT(time_took_sec_cached, time_took_sec_no_cache);

	std::cout << "Simplest code took seconds: " << time_took_sec_no_cache << " (no cache), and " << time_took_sec_cached << " (cached)." << std::endl,

	// Now checking that corresponding module is cached.
	found_in_cache = ScLLVMIsModuleCached(cur_dir, out_hash_str);
	ASSERT_TRUE(found_in_cache) << "Cached module is not found!";

	// Deleting cached module.
	bool deleted = ScLLVMDeleteCachedModule(cur_dir, out_hash_str);
	ASSERT_TRUE(deleted) << "Can't delete cached module!";

	// Trying to find again.
	found_in_cache = ScLLVMIsModuleCached(cur_dir, out_hash_str);
	ASSERT_FALSE(found_in_cache) << "Deleted cached module is found again!";
}

void TestScLLVMPerformance(bool delete_cached_modules) {

	void* codegen_engine = NULL;

	std::ifstream ifs("generated1.cpp");
	std::string orig_code_string((std::istreambuf_iterator<char>(ifs)), (std::istreambuf_iterator<char>()));

	std::vector<void*> exec_modules;
	std::vector<std::array<char, 65>> hashes;

	wchar_t cur_dir[1024];
	wchar_t* f = _wgetcwd(cur_dir, 1024);
	ASSERT_NE(f, nullptr) << "Can't get current working directory!";

	std::vector<void*> out_func_ptrs;

	const int32_t kNumCodegenFunctions = 100;
	float total_time_took_sec = 0;
	for (int32_t i = 0; i < kNumCodegenFunctions; i++) {

		std::string code_string = ReplaceString(orig_code_string, "12345", std::to_string(i));

		const char * const code = code_string.c_str();
		void* out_exec_module;
		void* out_func_ptr;

		float time_took_sec = 0;
		char out_hash_str[65];

		uint32_t err = ScLLVMProduceModule(
			cur_dir,
            nullptr,
			NULL,
			code,
			"gen_function",
			"external_dll1.dll;external_dll2.dll",
			true,
			nullptr,
			out_hash_str,
			&time_took_sec,
			&out_func_ptr,
			&out_exec_module,
			&codegen_engine);

		ASSERT_EQ(err, 0) << "ScLLVMProduceModule returned non-zero exit code!";

		total_time_took_sec += time_took_sec;

		std::array<char, 65> arr;
		std::copy(std::begin(out_hash_str), std::end(out_hash_str), std::begin(arr));
		hashes.push_back(arr);
		exec_modules.push_back(out_exec_module);

#ifdef USE_DLLS_DIRECTLY
		out_func_ptrs.push_back(FuncSimulation);
#else
		out_func_ptrs.push_back(out_func_ptr);
#endif
	}

	std::cout << "Average time to build code, seconds: " << total_time_took_sec / kNumCodegenFunctions << std::endl;

	clock_t start_time = clock();

	typedef int(*function_type) (int);

	int32_t k = 0;
	const int32_t kNumCallsEachFunction = 100000;
	for (auto &out_func_ptr : out_func_ptrs) {

		for (int32_t i = 0; i < kNumCallsEachFunction; i++) {

			int func_result = (function_type(out_func_ptr))(i);
			ASSERT_EQ(134786 + 265688 + k + i, func_result) << "Generated function returned wrong result!";
		}

		if (k % 10 == 0)
			std::cout << "Called: " << k << std::endl;

		k++;
	}

	clock_t end_time = clock();

	float seconds_time = (float)(end_time - start_time) / CLOCKS_PER_SEC;

	std::cout << "Calling " << kNumCodegenFunctions << " functions " << kNumCallsEachFunction << " times took seconds: " << seconds_time << std::endl;

	// Checking modules cache.
	for (auto &e : hashes) {

		std::string hash_str(std::begin(e), std::end(e));

		// Now checking that corresponding module is cached.
		bool found_in_cache = ScLLVMIsModuleCached(cur_dir, hash_str.c_str());
		ASSERT_TRUE(found_in_cache) << "Cached module is not found!";

		if (delete_cached_modules) {

			// Deleting cached module.
			bool deleted = ScLLVMDeleteCachedModule(cur_dir, hash_str.c_str());
			ASSERT_TRUE(deleted) << "Can't delete cached module!";

			// Trying to find again.
			found_in_cache = ScLLVMIsModuleCached(cur_dir, hash_str.c_str());
			ASSERT_FALSE(found_in_cache) << "Deleted cached module is found again!";
		}
	}

	// Deleting modules.
	for (auto &e : exec_modules) {
		ScLLVMDeleteModule(codegen_engine, &e);
	}
}

TEST(ScLLVMTests, TestWithoutCachePerformance) {
	TestScLLVMPerformance(false);	
}

TEST(ScLLVMTests, TestWithCachePerformance) {
	TestScLLVMPerformance(true);
}

int main(int argc, char **argv) {

	// Initializing globally LLVM first.
	ScLLVMInit();

	::testing::InitGoogleTest(&argc, argv);
	return RUN_ALL_TESTS();
}