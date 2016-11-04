// Enabling asserts for release build as well.
#undef NDEBUG

#include "llvm/ExecutionEngine/MCJIT.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/Support/DynamicLibrary.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <iomanip>
#include <time.h>
#include <fstream>
#include <stdio.h>
#include <stdlib.h>
#include <stdexcept>
#include <string>
#include <cstring>
#include <codecvt>
#include <locale>
#include <sys/types.h>
#include <sys/stat.h>

#ifdef __cplusplus
extern "C" {
#endif

	// Version of this SCLLVM.
#ifdef _WIN32
	const wchar_t* const ScllvmVersion = L"2.1";
#else
	const char* const ScllvmVersion = "2.1";
#endif

#ifdef _WIN32
#include <direct.h>
#include <windows.h>
# define MODULE_API __declspec(dllexport)

#else
#include <unistd.h>
#include <sys/stat.h>
# define MODULE_API
#endif

	// Shifting operations.
#define SHA_SHIFT_RIGHT(x, n)    (x >> n)
#define SHA_ROTATE_RIGHT(x, n)   ((x >> n) | (x << ((sizeof(x) << 3) - n)))
#define SHA_CHANGE(x, y, z)  ((x & y) ^ (~x & z))
#define SHA_MAJ(x, y, z) ((x & y) ^ (x & z) ^ (y & z))

#define SHA256_COMB1(x) (SHA_ROTATE_RIGHT(x,  2) ^ SHA_ROTATE_RIGHT(x, 13) ^ SHA_ROTATE_RIGHT(x, 22))
#define SHA256_COMB2(x) (SHA_ROTATE_RIGHT(x,  6) ^ SHA_ROTATE_RIGHT(x, 11) ^ SHA_ROTATE_RIGHT(x, 25))
#define SHA256_COMB3(x) (SHA_ROTATE_RIGHT(x,  7) ^ SHA_ROTATE_RIGHT(x, 18) ^ SHA_SHIFT_RIGHT(x,  3))
#define SHA256_COMB4(x) (SHA_ROTATE_RIGHT(x, 17) ^ SHA_ROTATE_RIGHT(x, 19) ^ SHA_SHIFT_RIGHT(x, 10))

#define SHA_UNPACK_32(x, input) *((input) + 3) = (uint8_t) ((x)); *((input) + 2) = (uint8_t) ((x) >>  8); *((input) + 1) = (uint8_t) ((x) >> 16); *((input) + 0) = (uint8_t) ((x) >> 24);
#define SHA_PACK_32(input, x) *(x) = ((uint32_t) *((input) + 3)) | ((uint32_t) *((input) + 2) <<  8) | ((uint32_t) *((input) + 1) << 16) | ((uint32_t) *((input) + 0) << 24);

	// Standard constants for SHA-256.
	const static uint32_t kSha256Constants[64] =
	{
		0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
		0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
		0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
		0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
		0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
		0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
		0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
		0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
	};

	class SimpleSha256
	{
		static const uint32_t kShaBlockSize = 512 / 8;
		uint32_t total_length_;
		uint32_t cur_length_;
		uint8_t data_block_[2 * kShaBlockSize];
		uint32_t header_[8];

		void DoTransform(const uint8_t *input_data, uint32_t block_nb) {

			uint32_t t1, t2;
			int32_t j;
			uint32_t w[64];
			uint32_t wv[8];

			// Performing block processing.
			for (int32_t i = 0; i < (int32_t)block_nb; i++) {

				const uint8_t* sub_block = input_data + (i << 6);

				for (j = 0; j < 16; j++) {
					SHA_PACK_32(&sub_block[j << 2], &w[j]);
				}

				for (j = 16; j < 64; j++) {
					w[j] = SHA256_COMB4(w[j - 2]) + w[j - 7] + SHA256_COMB3(w[j - 15]) + w[j - 16];
				}
				for (j = 0; j < 8; j++) {
					wv[j] = header_[j];
				}
				for (j = 0; j < 64; j++) {

					t1 = wv[7] + SHA256_COMB2(wv[4]) + SHA_CHANGE(wv[4], wv[5], wv[6]) + kSha256Constants[j] + w[j];
					t2 = SHA256_COMB1(wv[0]) + SHA_MAJ(wv[0], wv[1], wv[2]);

					wv[7] = wv[6];
					wv[6] = wv[5];
					wv[5] = wv[4];
					wv[4] = wv[3] + t1;
					wv[3] = wv[2];
					wv[2] = wv[1];
					wv[1] = wv[0];
					wv[0] = t1 + t2;
				}

				for (j = 0; j < 8; j++) {
					header_[j] += wv[j];
				}
			}
		}
	public:

		void InitializeSha256() {

			header_[0] = 0x6a09e667;
			header_[1] = 0xbb67ae85;
			header_[2] = 0x3c6ef372;
			header_[3] = 0xa54ff53a;
			header_[4] = 0x510e527f;
			header_[5] = 0x9b05688c;
			header_[6] = 0x1f83d9ab;
			header_[7] = 0x5be0cd19;
			cur_length_ = 0;
			total_length_ = 0;
		}

		void UpdateSha256(const uint8_t *input_data, uint32_t len) {

			const uint8_t* shifted_data;
			uint32_t tmp_len = kShaBlockSize - cur_length_;
			uint32_t rem_len = len < tmp_len ? len : tmp_len;

			memcpy(&data_block_[cur_length_], input_data, rem_len);

			// Returning if its a tail non-complete block.
			if (cur_length_ + len < kShaBlockSize) {
				cur_length_ += len;
				return;
			}

			uint32_t new_len = len - rem_len;
			uint32_t block_size_current = new_len / kShaBlockSize;

			shifted_data = input_data + rem_len;

			DoTransform(data_block_, 1);
			DoTransform(shifted_data, block_size_current);

			rem_len = new_len % kShaBlockSize;
			memcpy(data_block_, &shifted_data[block_size_current << 6], rem_len);
			cur_length_ = rem_len;
			total_length_ += (block_size_current + 1) << 6;
		}

		void FinalizeSha256(uint8_t* hash_32bytes)
		{
			uint32_t block_size_current = (1 + ((kShaBlockSize - 9) < (cur_length_ % kShaBlockSize)));

			uint32_t b_length = (total_length_ + cur_length_) << 3;
			uint32_t pm_length = block_size_current << 6;
			memset(data_block_ + cur_length_, 0, pm_length - cur_length_);

			data_block_[cur_length_] = 0x80;
			SHA_UNPACK_32(b_length, data_block_ + pm_length - 4);
			DoTransform(data_block_, block_size_current);

			for (int32_t i = 0; i < 8; i++) {
				SHA_UNPACK_32(header_[i], &hash_32bytes[i << 2]);
			}
		}

		static std::string ProduceHash(std::string input)
		{
			const int32_t kSizeOfTheDigest = 32;

			uint8_t hash_32bytes[kSizeOfTheDigest];
			memset(hash_32bytes, 0, kSizeOfTheDigest);

			SimpleSha256 ctx = SimpleSha256();
			ctx.InitializeSha256();
			ctx.UpdateSha256((const uint8_t*)input.c_str(), input.length());
			ctx.FinalizeSha256(hash_32bytes);

			std::stringstream out_hash_str;
			out_hash_str << std::hex << std::setfill('0');

			for (int32_t i = 0; i < kSizeOfTheDigest; i++) {
				out_hash_str << std::setw(2) << (uint32_t)(hash_32bytes[i]);
			}

			return out_hash_str.str();
		}
	};

	class CodegenEngine
	{
		std::vector<llvm::ExecutionEngine*> exec_engines_;
		llvm::LLVMContext* llvm_context_;

	public:

		~CodegenEngine() {

			if (NULL != llvm_context_) {

				delete llvm_context_;
				llvm_context_ = NULL;
			}
		}

		CodegenEngine() {
			llvm_context_ = new llvm::LLVMContext();
		}

		void Cleanup(bool accumulate_old_modules) {

			if (!accumulate_old_modules) {

				while (!exec_engines_.empty()) {

					llvm::ExecutionEngine* eng = exec_engines_.back();
					exec_engines_.pop_back();
					delete eng;
				}
			}
		}

		void DestroyEngine(llvm::ExecutionEngine** exec_engine) {

			std::vector<llvm::ExecutionEngine*>::iterator position = std::find(exec_engines_.begin(), exec_engines_.end(), *exec_engine);

			// Checking if we have found this execution module, then removing it.
			if (position != exec_engines_.end()) {
				exec_engines_.erase(position);
				delete *exec_engine;
				*exec_engine = NULL;
			}
		}

		std::vector<std::string>& StringSplit(const std::string &s, char delim, std::vector<std::string> &elems) {
			std::stringstream ss(s);
			std::string item;
			while (std::getline(ss, item, delim)) {
				elems.push_back(item);
			}
			return elems;
		}

		std::vector<std::string> StringSplit(const std::string &s, char delim) {
			std::vector<std::string> elems;
			StringSplit(s, delim, elems);
			return elems;
		}

		// Creating directory path. 
#ifdef _WIN32
		static void CreateDirPath(const wchar_t* dir) {

			// Checking if directory exists.
			struct _stat64i32 dir_info;
			if (_wstat(dir, &dir_info) == 0)
				return;

			wchar_t tmp[1024];

			// Copying into temporary string.
			wcscpy(tmp, dir);
			size_t len = wcslen(tmp);

			// Checking for the last slash.
			if ((tmp[len - 1] == L'/') ||
				(tmp[len - 1] == L'\\')) {

				tmp[len - 1] = 0;
			}

			// Starting from the first character.
			for (wchar_t *p = tmp + 1; *p; p++) {

				// Checking if its a slash.
				if ((*p == L'/') || (*p == L'\\')) {

					*p = 0;

					// Making the directory.
					_wmkdir(tmp);

					*p = L'/';
				}
			}

			// Creating final directory.
			_wmkdir(tmp);
		}
#else
		static void CreateDirPath(const char* dir) {

			// Checking if directory exists.
			struct stat dir_info;
			if (stat(dir, &dir_info) == 0)
				return;

			char tmp[1024];

			// Copying into temporary string.
			strcpy(tmp, dir);
			size_t len = strlen(tmp);

			// Checking for the last slash.
			if ((tmp[len - 1] == '/') ||
				(tmp[len - 1] == '\\')) {

				tmp[len - 1] = 0;
			}

			// Starting from the first character.
			for (char *p = tmp + 1; *p; p++) {

				// Checking if its a slash.
				if ((*p == '/') || (*p == '\\')) {

					*p = 0;

					// Making the directory.
					mkdir(tmp, 0700);

					*p = '/';
				}
			}

			// Creating final directory.
			mkdir(tmp, 0700);
		}

#endif

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

		uint32_t ProduceModuleAndReturnPointers(
#ifdef _WIN32
			const wchar_t* const path_to_cache_dir,
#else
			const char* const path_to_cache_dir,
#endif
			const char* const predefined_hash_str,
			const char* const code_to_build,
			const char* const function_names_delimited,
			const char* const ext_libraries_names_delimited,
			const bool delete_sources,
			const char* const predefined_clang_params,
			char* const out_hash_65bytes,
			float* const out_time_seconds,
			uint64_t out_func_ptrs[],
			void** out_exec_module) {

			clock_t start_time = clock();

			using namespace llvm;

			std::string code_string = code_to_build;

			std::vector<std::string> function_names = StringSplit(function_names_delimited, ';');
			assert((function_names.size() > 0) && "At least one function should be supplied.");

			// Setting all output pointers to NULL to avoid dirty values on error. 
			for (size_t i = 0; i < function_names.size(); i++) {
				out_func_ptrs[i] = 0;
			}

			std::vector<std::string> ext_library_names;
			std::string ext_libraries_names_delimited_string;
			if (NULL != ext_libraries_names_delimited)
				ext_libraries_names_delimited_string = ext_libraries_names_delimited;

			if (!ext_libraries_names_delimited_string.empty()) {
				ext_library_names = StringSplit(ext_libraries_names_delimited_string, ';');
				assert((ext_library_names.size() > 0) && "At least one external library should be supplied, if not NULL.");
			}

			std::string error_str;

			// Create some module to put our function into it. 
			std::unique_ptr<llvm::Module> owner = make_unique<llvm::Module>("test", *llvm_context_);

			llvm::ExecutionEngine* exec_engine = llvm::EngineBuilder(std::move(owner))
				.setErrorStr(&error_str)
				.setEngineKind(EngineKind::JIT)
				.setOptLevel(CodeGenOpt::Aggressive)
				.create();

			if (nullptr == exec_engine) {
				std::cout << "Can't create LLVM execution engine. Error: " << error_str;
				assert(false && "Can't create LLVM execution engine.");
			}

			std::string file_name_no_ext;

			// Checking if hash is given. 
			if (nullptr == predefined_hash_str) {

				// Calculating hash from input code. 
				std::string hash_str = SimpleSha256::ProduceHash(code_string);

				// Saving hash if needed.
				if (nullptr != out_hash_65bytes) {
					std::strcpy(out_hash_65bytes, hash_str.c_str());
				}

				file_name_no_ext = hash_str;
			}
			else {

				file_name_no_ext = predefined_hash_str;
			}

			int32_t err_code;

#ifdef _WIN32
			std::wstring path_to_cache_dir_versioned = path_to_cache_dir;
			path_to_cache_dir_versioned += L"/";
			path_to_cache_dir_versioned += ScllvmVersion;
#else
			std::string path_to_cache_dir_versioned = path_to_cache_dir;
			path_to_cache_dir_versioned += "/";
			path_to_cache_dir_versioned += ScllvmVersion;
#endif

			// Creating directory path. 
			CreateDirPath(path_to_cache_dir_versioned.c_str());

#ifdef _WIN32
			std::wstringstream tmp;
			tmp << path_to_cache_dir_versioned << "/" << file_name_no_ext.c_str();
			std::wstring obj_file_path = tmp.str();
			tmp.str(L"");
			tmp.clear();
			tmp << path_to_cache_dir_versioned << "/" << file_name_no_ext.c_str() << ".cpp";
			std::wstring cpp_file_path = tmp.str();
#else
			std::string obj_file_path = path_to_cache_dir_versioned + "/" + file_name_no_ext;
			std::string cpp_file_path = path_to_cache_dir_versioned + "/" + file_name_no_ext + ".cpp";
#endif

			std::ifstream f(obj_file_path);

			// Checking if object file does not exist. 
			if (!f.good()) {

				// Saving source file to disk. 
				std::ofstream temp_cpp_file(cpp_file_path);
				temp_cpp_file << "#undef _MSC_VER\n";
				temp_cpp_file << code_string;
				temp_cpp_file.close();

#ifdef _WIN32
				std::wstringstream clang_cmd_stream;
#else
				std::stringstream clang_cmd_stream;
#endif

				// Checking if we have custom clang parameters from the user.
				if ((NULL != predefined_clang_params) && ('\0' != predefined_clang_params)) {
					clang_cmd_stream << "clang++ -c -mcmodel=large " << predefined_clang_params << " \"" << cpp_file_path << "\" -o \"" << obj_file_path << "\"";
				}
				else {
					clang_cmd_stream << "clang++ -O3 -c -mcmodel=large \"" << cpp_file_path << "\" -o \"" << obj_file_path << "\"";
				}

				// Generating new object file.
#ifdef _WIN32
				std::wstring clang_cmd = clang_cmd_stream.str();
				err_code = _wsystem(clang_cmd.c_str());
#else
				std::string clang_cmd = clang_cmd_stream.str();
				err_code = system(clang_cmd.c_str());
#endif

				assert((0 == err_code) && "clang++ returned an error while compiling generated code.");

				// Deleting source file if necessary. 
				if (delete_sources) {
#ifdef _WIN32
					err_code = _wremove(cpp_file_path.c_str());
#else
					err_code = remove(cpp_file_path.c_str());
#endif

					assert((0 == err_code) && "Deleting the source file returned an error.");
				}
			}

			// Loading the object file. 
#ifdef _WIN32

			// Same as for Linux, LLVM operates with UTF-8 strings, so we need to convert from wide string.
			std::wstring_convert<std::codecvt_utf8<wchar_t>> utf8_conv;
			std::string utf8_obj_file_path = utf8_conv.to_bytes(obj_file_path);
			llvm::Expected<object::OwningBinary<object::ObjectFile>> obj_file =
				object::ObjectFile::createObjectFile(utf8_obj_file_path);
#else
			llvm::Expected<object::OwningBinary<object::ObjectFile>> obj_file =
				object::ObjectFile::createObjectFile(obj_file_path);
#endif
			assert((obj_file) && "Can't load given object file.");

			object::OwningBinary<object::ObjectFile> &obj_file_ref = obj_file.get();
			exec_engine->addObjectFile(std::move(obj_file_ref));

			// Loading external library.
			for (size_t i = 0; i < ext_library_names.size(); i++) {
				bool library_error = llvm::sys::DynamicLibrary::LoadLibraryPermanently(ext_library_names[i].c_str());
				assert((!library_error) && "Can't load given dynamic library.");
			}

			// Finalizing MCJIT execution engine (does relocation). 
			exec_engine->finalizeObject();

			// Getting pointer for each function. 
			for (size_t i = 0; i < function_names.size(); i++) {

				// Obtaining the pointer to created function. 
				out_func_ptrs[i] = exec_engine->getFunctionAddress(function_names[i]);
				assert((0 != out_func_ptrs[i]) && "Can't get function address from module! Dependencies issue?");
			}

			// Adding to the list of execution engines. 
			exec_engines_.push_back(exec_engine);

			// Saving execution engine for later use. 
			*out_exec_module = exec_engine;

			clock_t end_time = clock();
			float time_took = (float)(end_time - start_time) / CLOCKS_PER_SEC;
			if (nullptr != out_time_seconds)
				*out_time_seconds = time_took;

			return 0;
		}
	};

	// Global mutex.
	llvm::sys::MutexImpl* g_mutex;

	MODULE_API void ScLLVMInit() {

		g_mutex = new llvm::sys::MutexImpl();
		g_mutex->acquire();

		llvm::InitializeNativeTarget();
		llvm::InitializeNativeTargetAsmPrinter();

		g_mutex->release();
	}

	MODULE_API void ScLLVMDeleteModule(CodegenEngine* const codegen_engine, void** scllvm_module) {

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		codegen_engine->DestroyEngine((llvm::ExecutionEngine**) scllvm_module);

		g_mutex->release();
	}

	MODULE_API uint32_t ScLLVMCalculateHash(
		const char* const code_to_build,
		char* const out_hash_65bytes
	) {
		// Checking that string is not empty.
		if (std::strlen(code_to_build) < 1) {
			return 1;
		}

		std::string hash_str = SimpleSha256::ProduceHash(code_to_build);

		if (nullptr != out_hash_65bytes) {
			std::strcpy(out_hash_65bytes, hash_str.c_str());
		}

		return 0;
	}

	MODULE_API uint32_t ScLLVMProduceModule(
#ifdef _WIN32
		const wchar_t* const path_to_cache_dir,
#else
		const char* const path_to_cache_dir,
#endif
		const char* const predefined_hash_str,
		const char* const code_to_build,
		const char* const function_names_delimited,
		const char* const ext_libraries_names_delimited,
		const bool delete_sources,
		const char* const predefined_clang_params,
		char* const out_hash_65bytes,
		float* const out_time_seconds,
		uint64_t out_func_ptrs[],
		void** out_exec_module,
		CodegenEngine** const out_codegen_engine)
	{
		assert(nullptr != g_mutex);
		g_mutex->acquire();

		if (NULL == *out_codegen_engine) {
			*out_codegen_engine = new CodegenEngine();
		}

		uint32_t err_code = (*out_codegen_engine)->ProduceModuleAndReturnPointers(
			path_to_cache_dir,
			predefined_hash_str,
			code_to_build,
			function_names_delimited,
			ext_libraries_names_delimited,
			delete_sources,
			predefined_clang_params,
			out_hash_65bytes,
			out_time_seconds,
			out_func_ptrs,
			out_exec_module);

		g_mutex->release();

		return err_code;
	}

#ifdef _WIN32
	MODULE_API bool ScLLVMIsModuleCached(
		const wchar_t* const path_to_cache_dir,
		const char* const predefined_hash_str) {

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		// Converting hash name to wide char.
		std::string hash_string(predefined_hash_str);
		std::wstring hash_wstring(hash_string.begin(), hash_string.end());

		std::wstring obj_file_path = path_to_cache_dir;
		obj_file_path += L"/";
		obj_file_path += ScllvmVersion;
		obj_file_path += L"/";
		obj_file_path += hash_wstring;
		std::wifstream f(obj_file_path);

		g_mutex->release();

		if (f.good())
			return true;

		return false;
	}

	MODULE_API bool ScLLVMDeleteCachedModule(
		const wchar_t* const path_to_cache_dir,
		const char* const predefined_hash_str) {

		// Deleting if module is actually cached.
		if (!ScLLVMIsModuleCached(path_to_cache_dir, predefined_hash_str)) {
			return false;
		}

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		// Converting hash name to wide char.
		std::string hash_string(predefined_hash_str);
		std::wstring hash_wstring(hash_string.begin(), hash_string.end());

		std::wstring obj_file_path = path_to_cache_dir;
		obj_file_path += L"/";
		obj_file_path += ScllvmVersion;
		obj_file_path += L"/";
		obj_file_path += hash_wstring;

		// Deleting file.
		int err = _wremove(obj_file_path.c_str());

		g_mutex->release();

		return 0 == err;
	}

	MODULE_API bool ScLLVMDeleteAllCachedModulesInDir(
		const wchar_t* const path_to_cache_dir) {
		// todo
		return false;
	}

	MODULE_API bool ScLLVMDeleteCachedModulesInDirOlderThan(
		const wchar_t* const path_to_cache_dir,
		const int32_t older_than_days) {
		// todo
		return false;
	}
#else 
	MODULE_API bool ScLLVMIsModuleCached(
		const char* const path_to_cache_dir,
		const char* const predefined_hash_str) {

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		// Converting hash name to wide char.
		std::string hash_string(predefined_hash_str);
		std::string obj_file_path = path_to_cache_dir;
		obj_file_path += "/";
		obj_file_path += ScllvmVersion;
		obj_file_path += "/";
		obj_file_path += hash_string;
		std::ifstream f(obj_file_path);

		g_mutex->release();

		if (f.good())
			return true;

		return false;
	}

	MODULE_API bool ScLLVMDeleteCachedModule(
		const char* const path_to_cache_dir,
		const char* const predefined_hash_str) {

		// Deleting if module is actually cached.
		if (!ScLLVMIsModuleCached(path_to_cache_dir, predefined_hash_str)) {
			return false;
		}

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		// Converting hash name to wide char.
		std::string hash_string(predefined_hash_str);

		std::string obj_file_path = path_to_cache_dir;
		obj_file_path += "/";
		obj_file_path += ScllvmVersion;
		obj_file_path += "/";
		obj_file_path += hash_string;

		// Deleting file.
		int err = remove(obj_file_path.c_str());

		g_mutex->release();

		return 0 == err;
	}

	MODULE_API bool ScLLVMDeleteAllCachedModulesInDir(
		const char* const path_to_cache_dir) {
		// todo
		return false;
	}

	MODULE_API bool ScLLVMDeleteCachedModulesInDirOlderThan(
		const char* const path_to_cache_dir,
		const int32_t older_than_days) {
		// todo
		return false;
	}
#endif

	MODULE_API void ScLLVMDestroy(CodegenEngine* codegen_engine) {

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		assert((NULL != codegen_engine) && "Engine must exist to be destroyed!");
		codegen_engine->Cleanup(false);
		delete codegen_engine;

		g_mutex->release();
	}

	// Just a basic test.
	int32_t main() {
		ScLLVMInit();
		CodegenEngine* out_codegen_engine = nullptr;

		char out_hash_65bytes[65];
		float out_time_seconds;
		uint64_t out_func_ptrs[1] = { 0 };
		void* out_exec_module = nullptr;

		int32_t err_code = ScLLVMProduceModule(
#ifdef _WIN32
			L"star cÖunter",
#else
			"star cÖunter",
#endif
			nullptr,
			"extern \"C\" int gen_function(int p) { return p + 555; }",
			"gen_function",
			nullptr,
			false,
			nullptr, //"-O3 -c",
			out_hash_65bytes,
			&out_time_seconds,
			out_func_ptrs,
			&out_exec_module,
			&out_codegen_engine);

		assert(0 == err_code);
		
		typedef int(*function_type) (int);
		function_type gen_func = (function_type)(out_func_ptrs[0]);

		int32_t res = gen_func(3);

		assert(558 == res);

		ScLLVMDeleteModule(out_codegen_engine, &out_exec_module);

		std::cout << "Test succeeded. Result: " << res << std::endl;

		return 0;
	}

#ifdef __cplusplus
}
#endif