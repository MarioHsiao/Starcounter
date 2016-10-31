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
	const wchar_t* const ScllvmVersion = L"2.0";
#else
	const char* const ScllvmVersion = "2.0";
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
					// TODO: Fix correct mode permissions.
					mkdir(tmp, 0777);

					*p = '/';
				}
			}

			// Creating final directory.
			// TODO: Fix correct mode permissions.
			mkdir(tmp, 0777);

			// Changing current directory to dir.
			err_code = chdir(dir);
			assert((0 == err_code) && "Can't change current directory to cache directory.");
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

		uint32_t CompileAndLoadObjectFile(
			const bool print_to_console,
			const bool do_optimizations,
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

		llvm::InitializeNativeTarget();
		llvm::InitializeNativeTargetAsmPrinter();
		llvm::InitializeNativeTargetAsmParser();
	}

	MODULE_API void ClangDeleteModule(CodegenEngine* const clang_engine, void** exec_engine) {

		assert(nullptr != g_mutex);
		g_mutex->acquire();

		clang_engine->DestroyEngine((llvm::ExecutionEngine**) exec_engine);

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

		ClangDestroy(cge);

		return 0;
	}

#ifdef __cplusplus
}
#endif