#include "clang/Basic/TargetInfo.h"
#include "clang/Lex/Preprocessor.h"
#include "clang/Frontend/CompilerInstance.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Parse/ParseAST.h"
#include "clang/CodeGen/ModuleBuilder.h"
#include "llvm/ExecutionEngine/ExecutionEngine.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/IR/LLVMContext.h"
#include "llvm/ExecutionEngine/SectionMemoryManager.h"
#include "llvm/ExecutionEngine/MCJIT.h"
#include "llvm/Transforms/IPO.h"
#include "llvm/Transforms/IPO/PassManagerBuilder.h"
#include "llvm/IR/LegacyPassManager.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>
#include <stdio.h>
#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

	// Version of this SCLLVM.
#ifdef _WIN32
	const wchar_t* const ScllvmVersion = L"2.2";
#else
	const char* const ScllvmVersion = "2.2";
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

<<<<<<< HEAD
=======
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

	// Is scllvm diagnostics on? 
	bool g_diag_on = false;

>>>>>>> develop
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

		uint32_t CompileCodeAndGetFuntions(
			const bool accumulate_old_modules,
			const bool print_to_console,
			const bool do_optimizations,
			const char* const input_code_str,
			const char* const function_names_delimited,
			uint64_t out_func_ptrs[],
			void** out_exec_engine)
		{
			using namespace clang;
			using namespace llvm;

			clock_t start_parsing = clock();

			std::string code_string = input_code_str;
			std::vector<std::string> function_names = StringSplit(function_names_delimited, ';');
			assert(function_names.size() > 0);

			// Setting all output pointers to NULL to avoid dirty values on error.
			for (size_t i = 0; i < function_names.size(); i++) {
				out_func_ptrs[i] = 0;
			}

			// Performing cleanup before the new round.
			Cleanup(accumulate_old_modules);

			CompilerInstance ci;
			CodeGenOptions code_gen_options;
			code_gen_options.DisableFree = 0;

			if (do_optimizations) {
				code_gen_options.OptimizationLevel = 3; // All optimizations.
			}
			else {
				code_gen_options.OptimizationLevel = 0; // No optimizations.
				code_gen_options.OptimizeSize = 0;
				code_gen_options.NoInline = 1;
			}

			std::shared_ptr<clang::TargetOptions> target_options(new clang::TargetOptions());

			// NOTE: Needed to resolve LLVM ERROR: Incompatible object format!
			target_options->Triple = sys::getDefaultTargetTriple() + "-elf";

			IntrusiveRefCntPtr<DiagnosticOptions> diagnostic_options = new DiagnosticOptions();
			DiagnosticConsumer* diagnostic_client;
			if (print_to_console) {
				diagnostic_client = new TextDiagnosticPrinter(errs(), &*diagnostic_options);
			}
			else {
				diagnostic_client = new TextDiagnosticBuffer();
			}

			IntrusiveRefCntPtr<DiagnosticIDs> diagnostic_id(new DiagnosticIDs());
			IntrusiveRefCntPtr<DiagnosticsEngine> diagnostic_engine =
				new DiagnosticsEngine(diagnostic_id, &*diagnostic_options, &*diagnostic_client);

			ci.setDiagnostics(&*diagnostic_engine);

			TargetInfo *pti = TargetInfo::CreateTargetInfo(ci.getDiagnostics(), target_options);
			ci.setTarget(pti);

			LangOptions& lang_options = ci.getLangOpts();

			lang_options.Bool = 1;
			lang_options.CPlusPlus = 1;
			lang_options.CPlusPlus11 = 1;
			lang_options.CPlusPlus14 = 1;
			lang_options.CPlusPlus1z = 1;
			lang_options.LineComment = 1;
			lang_options.CXXOperatorNames = 1;
			lang_options.ConstStrings = 1;
			lang_options.Exceptions = 1;
			lang_options.CXXExceptions = 1;

			lang_options.SpellChecking = 0;

			if (do_optimizations) {
				lang_options.Optimize = 1;
			}
			else {
				lang_options.Optimize = 0;
			}

			ci.getCodeGenOpts() = code_gen_options;
			ci.createFileManager();
			ci.createSourceManager(ci.getFileManager());
			ci.createPreprocessor(clang::TU_Prefix);
			ci.getPreprocessorOpts().UsePredefines = false;
			ci.getFrontendOpts().DisableFree = 0;
			ci.getDiagnostics().setIgnoreAllWarnings(false);
			ci.getDiagnosticOpts().IgnoreWarnings = 1;

			CodeGenerator* codegen_ = CreateLLVMCodeGen(
				ci.getDiagnostics(),
				"test",
				ci.getHeaderSearchOpts(),
				ci.getPreprocessorOpts(),
				ci.getCodeGenOpts(),
				*llvm_context_);

			// Enabling Clang intrinsics.
			Preprocessor& pp = ci.getPreprocessor();
			pp.getBuiltinInfo().initializeBuiltins(pp.getIdentifierTable(), pp.getLangOpts());

			std::unique_ptr<MemoryBuffer> mb = MemoryBuffer::getMemBufferCopy(code_string, "some");
			assert(mb && "Error creating MemoryBuffer!");

			ci.setASTConsumer(std::unique_ptr<ASTConsumer>(codegen_));
			ci.createASTContext();

			clang::FileID main_file_id = ci.getSourceManager().createFileID(std::move(mb));
			ci.getSourceManager().setMainFileID(main_file_id);
			ci.getDiagnosticClient().BeginSourceFile(lang_options);
			ParseAST(ci.getPreprocessor(), codegen_, ci.getASTContext());
			ci.getDiagnosticClient().EndSourceFile();

			clock_t end_parsing = clock();

			clock_t start_jiting = clock();

			// Creating new module.
			llvm::Module* module = codegen_->ReleaseModule();
			assert(module && "Can't release module by some reason!");

			if (do_optimizations) {
				const int optLevel = 3;
				const int sizeLevel = 0;
				llvm::legacy::PassManager mpm;
				llvm::legacy::FunctionPassManager fpm(module);
				llvm::PassManagerBuilder builder;
				builder.OptLevel = optLevel;
				builder.SizeLevel = sizeLevel;
				builder.Inliner =
					llvm::createFunctionInliningPass(optLevel, sizeLevel);
				builder.populateModulePassManager(mpm);
				builder.populateFunctionPassManager(fpm);

				auto fi = module->functions();
				fpm.doInitialization();
				for (Function &f : fi) fpm.run(f);
				fpm.doFinalization();

				mpm.run(*module);
			}

			std::string error_str;

			llvm::ExecutionEngine* exec_engine = NULL;

			if (do_optimizations) {

				exec_engine = llvm::EngineBuilder(std::unique_ptr<llvm::Module>(module))
					.setErrorStr(&error_str)
					.setCodeModel(llvm::CodeModel::Large)
					.setRelocationModel(llvm::Reloc::Static)
					.setMCJITMemoryManager(llvm::make_unique<SectionMemoryManager>())
					.setEngineKind(EngineKind::JIT)
					.setOptLevel(CodeGenOpt::Aggressive)
					.create();

			}
			else {

				exec_engine = llvm::EngineBuilder(std::unique_ptr<llvm::Module>(module))
					.setErrorStr(&error_str)
					.setCodeModel(llvm::CodeModel::Large)
					.setRelocationModel(llvm::Reloc::Static)
					.setMCJITMemoryManager(llvm::make_unique<SectionMemoryManager>())
					.setEngineKind(EngineKind::JIT)
					.setOptLevel(CodeGenOpt::None)
					.create();
			}

			assert((NULL != exec_engine) && "Can't create execution engine by some reason!");

			// Setting module data layout as from execution engine.
			module->setDataLayout(exec_engine->getDataLayout());

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

			clock_t end_jiting = clock();

			float seconds_parsing = (float)(end_parsing - start_parsing) / CLOCKS_PER_SEC,
				seconds_jiting = (float)(end_jiting - start_jiting) / CLOCKS_PER_SEC;

			if (print_to_console) {
				std::cout << "Codegen took seconds: " << seconds_parsing + seconds_jiting << " (parsing " << seconds_parsing << ", jitting " << seconds_jiting << ")." << std::endl;
			}

			return 0;
		}

		// Creating directory recursively and making it current. 
#ifdef _WIN32
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
#else
		static void CreateDirAndSwitch(const char* dir) {

			// Immediately trying to switch to dir.
			int err_code = chdir(dir);
			if (0 == err_code) {
				return;
			}

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
<<<<<<< HEAD
			// TODO: Fix correct mode permissions.
			mkdir(tmp, 0777);

			// Changing current directory to dir.
			err_code = chdir(dir);
			assert((0 == err_code) && "Can't change current directory to cache directory.");
=======
			mkdir(tmp, 0700);
>>>>>>> develop
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

<<<<<<< HEAD
		uint32_t CompileAndLoadObjectFile(
			const bool print_to_console,
			const bool do_optimizations,
=======
		uint32_t ProduceModuleAndReturnPointers(
>>>>>>> develop
#ifdef _WIN32
			const wchar_t* const path_to_cache_dir,
#else
			const char* const path_to_cache_dir,
#endif
			const char* const predefined_hash_str,
			const char* const input_code_chars,
			const char* const function_names_delimited,
			const bool delete_sources,
			uint64_t out_func_ptrs[],
			void** out_exec_engine) {

			clock_t start_time = clock();

			std::vector<std::string> function_names = StringSplit(function_names_delimited, ';');
			assert((function_names.size() > 0) && "At least one function should be supplied.");

			// Setting all output pointers to NULL to avoid dirty values on error.
			for (size_t i = 0; i < function_names.size(); i++) {
				out_func_ptrs[i] = 0;
			}

			std::string out_file_name_no_ext;
			std::string input_code_str = input_code_chars;

			// Checking if hash is given.
			if (NULL == predefined_hash_str) {

				// Calculating hash from input code.
				std::size_t code_hash = std::hash<std::string>()(input_code_str);
				out_file_name_no_ext = std::to_string(code_hash);

			} else {

				out_file_name_no_ext = predefined_hash_str;
			}

			// Saving path to current directory.
			wchar_t saved_current_dir[1024];
			_wgetcwd(saved_current_dir, 1024);

			// Creating cache directory.
			CreateDirAndSwitch(path_to_cache_dir);

#ifdef _WIN32
			std::string dll_file_name = out_file_name_no_ext + ".dll";
			std::string linker_name = "lld-link";
#else
			std::string dll_file_name = out_file_name_no_ext + ".so";
			std::string linker_name = "lld-ld";
#endif

			// Checking if generated library exists.
			std::ifstream f(dll_file_name);
			if (!f.good()) {

#ifdef _WIN32
				input_code_str = ReplaceString(input_code_str, "extern \"C\"", "extern \"C\" __declspec(dllexport)");
#endif

				std::string cpp_file_name = out_file_name_no_ext + ".cpp";

				// Saving source file to disk.
				std::ofstream temp_cpp_file(cpp_file_name);
				temp_cpp_file << input_code_str;

<<<<<<< HEAD
				// Adding library entry at the end.
				temp_cpp_file << "\nextern \"C\" int dllentry() { return 1; }\n";
				temp_cpp_file.close();

				// Creating command line for clang.
				std::stringstream clang_cmd;
				clang_cmd << "clang++ -O3 -c -march=x86-64 " << cpp_file_name << " -o " << out_file_name_no_ext << ".o";

				// Generating new object file.
				std::cout << clang_cmd.str() << std::endl;
				int32_t err_code = system(clang_cmd.str().c_str());
				assert((0 == err_code) && "clang++ returned an error while compiling generated code.");

				// Deleting source file if necessary.
=======
				// Printing diagnostics.
				if (g_diag_on) {
#ifdef _WIN32
					std::wcout << "[scllvm]: module is not cached, creating it: \"" << obj_file_path << "\"" << std::endl;
#else
					std::cout << "[scllvm]: module is not cached, creating it: \"" << obj_file_path << "\"" << std::endl;
#endif
				}

				// Saving source file to disk. 
				std::ofstream temp_cpp_file(cpp_file_path);
				temp_cpp_file << code_string;
				temp_cpp_file.close();

#ifndef _WIN32
				chmod(cpp_file_path.c_str(), 0600);
#endif

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
				if (g_diag_on) {
					std::wcout << "[scllvm]: running clang tool: " << clang_cmd << std::endl;
				}

				err_code = _wsystem(clang_cmd.c_str());
#else
				std::string clang_cmd = clang_cmd_stream.str();
				if (g_diag_on) {
					std::cout << "[scllvm]: running clang tool: " << clang_cmd << std::endl;
				}

				err_code = system(clang_cmd.c_str());
#endif

				assert((0 == err_code) && "clang++ returned an error while compiling generated code.");

#ifndef _WIN32
				chmod(obj_file_path.c_str(), 0600);
#endif

				// Deleting source file if necessary. 
>>>>>>> develop
				if (delete_sources) {
					err_code = remove(cpp_file_name.c_str());
					assert((0 == err_code) && "Deleting the source file returned an error.");
				}

				// Creating command line for lld.
				std::stringstream lld_cmd;
				lld_cmd << linker_name << " /dll /entry:dllentry /opt:lldlto=3 " << out_file_name_no_ext << ".o";

				// Generating new object file.
				std::cout << lld_cmd.str() << std::endl;
				err_code = system(lld_cmd.str().c_str());
				assert((0 == err_code) && "lld returned an error while compiling generated code.");
			}

			
#ifdef _WIN32
			// Loading library into memory.
			HMODULE dll_handle = LoadLibrary(dll_file_name.c_str());
			assert(dll_handle != NULL);

			// Getting pointer for each function.
			for (size_t i = 0; i < function_names.size(); i++) {

				// Obtaining the pointer to created function.
				out_func_ptrs[i] = (uint64_t) GetProcAddress(dll_handle, function_names[i].c_str());
				assert((0 != out_func_ptrs[i]) && "Can't get function address from loaded library!");
			}

			// Saving execution engine for later use.
			*out_exec_engine = dll_handle;
#endif

			float seconds_time = (float)(clock() - start_time) / CLOCKS_PER_SEC;

			if (print_to_console) {
				std::cout << "Procedure took seconds: " << seconds_time << std::endl;
			}

			// Changing current directory back to original.
			_wchdir(saved_current_dir);

			return 0;
		}
	};

	// Global mutex.
	llvm::sys::MutexImpl* g_mutex;

	MODULE_API void ClangInit() {

        g_mutex = new llvm::sys::MutexImpl();

		// Checking if we have diagnostics on.
		char* scllvm_diag_var = getenv("SCLLVM_DIAG_ON");
		if (nullptr != scllvm_diag_var) {

			// Assuming if T is first letter than its True value.
			if ((scllvm_diag_var[0] == 't') ||
				(scllvm_diag_var[0] == 'T')) {
				g_diag_on = true;
			}			
		}

		llvm::InitializeNativeTarget();
		llvm::InitializeNativeTargetAsmPrinter();
		llvm::InitializeNativeTargetAsmParser();
	}

<<<<<<< HEAD
	MODULE_API void ClangDeleteModule(CodegenEngine* const clang_engine, void** exec_engine) {
=======
	MODULE_API void ScLLVMDeleteModule(CodegenEngine* const codegen_engine, void** scllvm_module) {
>>>>>>> develop

		assert(nullptr != g_mutex);
		g_mutex->acquire();

<<<<<<< HEAD
		clang_engine->DestroyEngine((llvm::ExecutionEngine**) exec_engine);
=======
		codegen_engine->DestroyEngine((llvm::ExecutionEngine**) scllvm_module);
>>>>>>> develop

		g_mutex->release();
	}

	MODULE_API uint32_t ClangCompileCodeAndGetFuntions(
		CodegenEngine** const clang_engine,
		const bool accumulate_old_modules,
		const bool print_to_console,
		const bool do_optimizations,
		const char* const input_code_str,
		const char* const function_names_delimited,
		uint64_t out_func_ptrs[],
		void** out_exec_engine)
	{
        assert(nullptr != g_mutex);
        g_mutex->acquire();

		if (NULL == *clang_engine) {
			*clang_engine = new CodegenEngine();
		}

        uint32_t err_code = (*clang_engine)->CompileCodeAndGetFuntions(
			accumulate_old_modules,
			print_to_console,
			do_optimizations,
			input_code_str,
			function_names_delimited,
			out_func_ptrs,
			out_exec_engine);

        g_mutex->release();

        return err_code;
	}

	MODULE_API uint32_t ClangCompileAndLoadObjectFile(
		CodegenEngine** const clang_engine,
		const bool print_to_console,
		const bool do_optimizations,
#ifdef _WIN32
		const wchar_t* const path_to_cache_dir,
#else
		const char* const path_to_cache_dir,
#endif
		const char* const predefined_hash_str,
		const char* const input_code_str,
		const char* const function_names_delimited,
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

	int main() {

		ClangInit();

		CodegenEngine* cge = NULL;

		/*
		std::ifstream ifs("a3jmo3vhkidakmq.cpp");
		std::string code((std::istreambuf_iterator<char>(ifs)), (std::istreambuf_iterator<char>()));
		const char * const code = code.c_str();
		const char * const function_names = "InitGeneratedLib;GetInitGeneratedLib";
		*/

		const char * const code = "extern \"C\" int Func1(int x) { return x + 1; }\n"
			"extern \"C\" void UseIntrinsics() { asm(\"int3\");  __builtin_unreachable(); }";

		const char * const function_names = "Func1;UseIntrinsics";

		uint64_t out_func_ptrs[2];
		void* exec_engine;

		uint32_t err = ClangCompileCodeAndGetFuntions(&cge, false, true, true, code, function_names, out_func_ptrs, &exec_engine);
		if (err) {
			assert(!"ClangCompileCodeAndGetFuntions returned non-zero exit code!");
		}

		typedef int(*function_type) (int);
		int func_result = (function_type(out_func_ptrs[0]))(132);
		if (133 != func_result) {
			assert(!"Generated increment function returned wrong result!");
		}

		std::cout << "Simple code generation test passed!" << std::endl;

		// Testing keeping modules.
		for (int n = 0; n < 100; n++) {

			clock_t begin = clock();

			for (int i = 0; i < 1000; i++) {

				err = ClangCompileCodeAndGetFuntions(&cge, true, false, true, code, function_names, out_func_ptrs, &exec_engine);
				assert(0 == err);

				func_result = (function_type(out_func_ptrs[0]))(i);
				assert(i + 1 == func_result);
			}

			clock_t end = clock();
			double elapsed_ms = (double(end - begin) / CLOCKS_PER_SEC) * 1000.0;

			std::cout << "Passed accumulated gens: " << n * 1000 << ". Last 1000 gens took ms: " << (int32_t)elapsed_ms << std::endl;
		}

<<<<<<< HEAD
		ClangDestroy(cge);
=======
		ScLLVMDeleteModule(out_codegen_engine, &out_exec_module);

		std::cout << "Test succeeded. Result: " << res << std::endl;
>>>>>>> develop

		return 0;
	}

#ifdef __cplusplus
}
#endif