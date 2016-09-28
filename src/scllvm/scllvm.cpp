// Enabling asserts for release build as well.
#undef NDEBUG

#include "llvm/ExecutionEngine/MCJIT.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/support/DynamicLibrary.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>
#include <direct.h>
#include <stdio.h>
#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
#include <windows.h>
# define MODULE_API __declspec(dllexport)
#else
# define MODULE_API
#endif

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

		// Creating directory recursively and making it current. 
		static void CreateDirAndSwitch(const wchar_t* dir) {

			// Immediately trying to switch to dir.
			int err_code = _wchdir(dir);
			if (0 == err_code) {
				return;
			}

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

			// Changing current directory to dir.
			err_code = _wchdir(dir);
			assert((0 == err_code) && "Can't change current directory to cache directory.");
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

		uint32_t CompileAndLoadObjectFile(
			const bool print_to_console,
			const bool do_optimizations,
			const wchar_t* const path_to_cache_dir,
			const char* const predefined_hash_str,
			const char* const input_code_chars,
			const char* const function_names_delimited,
			const char* const ext_libraries_names_delimited,
			const bool delete_sources,
			uint64_t out_func_ptrs[],
			void** out_exec_engine) {

			clock_t start_time = clock();

			using namespace llvm;

			std::string code_string = input_code_chars;

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

			llvm::ExecutionEngine* exec_engine = NULL;

			if (do_optimizations) {

				exec_engine = llvm::EngineBuilder(std::move(owner))
					.setErrorStr(&error_str)
					.setEngineKind(EngineKind::JIT)
					.setOptLevel(CodeGenOpt::Aggressive)
					.create();
			}
			else {

				exec_engine = llvm::EngineBuilder(std::move(owner))
					.setErrorStr(&error_str)
					.setEngineKind(EngineKind::JIT)
					.setOptLevel(CodeGenOpt::None)
					.create();
			}

			assert((NULL != exec_engine) && "Can't create LLVM execution engine.");

			std::string file_name_no_ext;

			// Checking if hash is given. 
			if (NULL == predefined_hash_str) {

				// Calculating hash from input code. 
				std::size_t code_hash = std::hash<std::string>()(code_string);
				file_name_no_ext = std::to_string(code_hash);

			}
			else {

				file_name_no_ext = predefined_hash_str;
			}

			int32_t err_code;

			wchar_t saved_original_dir[1024];
			_wgetcwd(saved_original_dir, 1024);

			// Creating directory and switching. 
			CreateDirAndSwitch(path_to_cache_dir);

			std::string obj_file_name = file_name_no_ext + ".o";
			std::ifstream f(obj_file_name);

			// Checking if object file does not exist. 
			if (!f.good()) {

				// Adding cpp file extension. 
				std::string cpp_file_name = file_name_no_ext + ".cpp";

				// Saving source file to disk. 
				std::ofstream temp_cpp_file(cpp_file_name);
				temp_cpp_file << code_string;
				temp_cpp_file.close();

				// Creating command line for clang. 
				std::stringstream clang_cmd;
				clang_cmd << "clang++.exe -O3 -c -mcmodel=large " << cpp_file_name << " -o " << obj_file_name;

				// Generating new object file. 
				err_code = system(clang_cmd.str().c_str());
				assert((0 == err_code) && "clang++ returned an error while compiling generated code.");

				// Deleting source file if necessary. 
				if (delete_sources) {

					err_code = remove(cpp_file_name.c_str());
					assert((0 == err_code) && "Deleting the source file returned an error.");
				}
			}

			// Loading the object file. 
			llvm::Expected<object::OwningBinary<object::ObjectFile>> obj_file =
				object::ObjectFile::createObjectFile(obj_file_name.c_str());

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
				assert((0 != out_func_ptrs[i]) && "Can't get function address from JITed code!");
			}

			// Adding to the list of execution engines. 
			exec_engines_.push_back(exec_engine);

			// Saving execution engine for later use. 
			*out_exec_engine = exec_engine;

			clock_t end_time = clock();

			float seconds_time = (float)(end_time - start_time) / CLOCKS_PER_SEC;

			if (print_to_console) {
				std::cout << "Loading object took seconds: " << seconds_time << std::endl;
			}

			// Changing current directory back to original. 
			_wchdir(saved_original_dir);

			return 0;
		}
	};

    // Global mutex.
    llvm::sys::MutexImpl* g_mutex;

	MODULE_API void ClangInit() {

        g_mutex = new llvm::sys::MutexImpl();

		llvm::InitializeNativeTarget();
		llvm::InitializeNativeTargetAsmPrinter();
	}

	MODULE_API void ClangDeleteModule(CodegenEngine* const clang_engine, void** exec_engine) {

        assert(nullptr != g_mutex);
        g_mutex->acquire();

		clang_engine->DestroyEngine((llvm::ExecutionEngine**) exec_engine);

        g_mutex->release();
	}

	MODULE_API uint32_t ClangCompileAndLoadObjectFile(
		CodegenEngine** const clang_engine,
		const bool print_to_console,
		const bool do_optimizations,
		const wchar_t* const path_to_cache_dir,
		const char* const predefined_hash_str,
		const char* const input_code_str,
		const char* const function_names_delimited,
		const char* const ext_libraries_names_delimited,
		const bool delete_sources,
		uint64_t out_func_ptrs[],
		void** out_exec_engine)
	{
		assert(nullptr != g_mutex);
		g_mutex->acquire();

		if (NULL == *clang_engine) {
			*clang_engine = new CodegenEngine();
		}

		uint32_t err_code = (*clang_engine)->CompileAndLoadObjectFile(
			print_to_console,
			do_optimizations,
			path_to_cache_dir,
			predefined_hash_str,
			input_code_str,
			function_names_delimited,
			ext_libraries_names_delimited,
			delete_sources,
			out_func_ptrs,
			out_exec_engine);

		g_mutex->release();

		return err_code;
	}

	MODULE_API void ClangDestroy(CodegenEngine* clang_engine) {

		assert((NULL != clang_engine) && "Engine must exist to be destroyed!");

		clang_engine->Cleanup(false);

		delete clang_engine;
	}

#ifdef __cplusplus
}
#endif