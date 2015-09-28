#include "clang/Basic/TargetInfo.h"
#include "clang/lex/Preprocessor.h"
#include "clang/Frontend/CompilerInstance.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Parse/ParseAST.h"
#include "clang/codegen/ModuleBuilder.h"
#include "llvm/ExecutionEngine/ExecutionEngine.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/IR/LLVMContext.h"
#include "llvm/ExecutionEngine/SectionMemoryManager.h"
#include "llvm/ExecutionEngine/MCJIT.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>

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
		for (int i = 0; i < function_names.size(); i++) {
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
		lang_options.GNUMode = 1;
		lang_options.CXXExceptions = 1;
		lang_options.RTTI = 1;
		lang_options.Bool = 1;
		lang_options.CPlusPlus = 1;
		lang_options.CPlusPlus11 = 1;

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
		pp.getBuiltinInfo().InitializeBuiltins(pp.getIdentifierTable(), pp.getLangOpts());

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
		module->setDataLayout(*exec_engine->getDataLayout());

		// Finalizing MCJIT execution engine (does relocation).
		exec_engine->finalizeObject();

		// Getting pointer for each function.
		for (int32_t i = 0; i < function_names.size(); i++) {

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

		std::cout << "Codegen took seconds: " << seconds_parsing + seconds_jiting << " (" << seconds_parsing << ", " << seconds_jiting << ")." << std::endl;

		return 0;
	}
};

extern "C" __declspec(dllexport) void ClangInit() {
	llvm::InitializeNativeTarget();
	llvm::InitializeNativeTargetAsmPrinter();
	llvm::InitializeNativeTargetAsmParser();
}

extern "C" __declspec(dllexport) void ClangDeleteModule(CodegenEngine* const clang_engine, void** exec_engine) {
	clang_engine->DestroyEngine((llvm::ExecutionEngine**) exec_engine);
}

extern "C" __declspec(dllexport) uint32_t ClangCompileCodeAndGetFuntions(
	CodegenEngine** const clang_engine,
	const bool accumulate_old_modules,
	const bool print_to_console,
	const bool do_optimizations,
	const char* const input_code_str,
	const char* const function_names_delimited,
	uint64_t out_func_ptrs[],
	void** out_exec_engine)
{
	if (NULL == *clang_engine) {
		*clang_engine = new CodegenEngine();
	}

	return (*clang_engine)->CompileCodeAndGetFuntions(
		accumulate_old_modules,
		print_to_console,
		do_optimizations,
		input_code_str,
		function_names_delimited,
		out_func_ptrs,
		out_exec_engine);
}

extern "C" __declspec(dllexport) void ClangDestroy(CodegenEngine* clang_engine) {

	assert((NULL != clang_engine) && "Engine must exist to be destroyed!");

	clang_engine->Cleanup(false);

	delete clang_engine;
}

int main() {

	ClangInit();

	CodegenEngine* cge = NULL;

	std::ifstream ifs("c:\\Users\\Alexey Moiseenko\\Downloads\\a3jmo3vhkidakmq.cpp");
	std::string code2((std::istreambuf_iterator<char>(ifs)),
		(std::istreambuf_iterator<char>()));

	const char * const code = code2.c_str(); /*"extern \"C\" int Func1(int x) { return x + 1; }"
											 "extern \"C\" void UseIntrinsics() { asm(\"int3\");  __builtin_unreachable(); }";*/
	const char * const function_names = "InitGeneratedLib;GetInitGeneratedLib"; //"Func1;UseIntrinsics";
	uint64_t out_func_ptrs[2];
	void* exec_engine;
	ClangCompileCodeAndGetFuntions(&cge, false, true, true, code, function_names, out_func_ptrs, &exec_engine);

	// Calling test function.
	typedef void(*function_type1) (uint32_t tls_key_context, const uint8_t **segment_base_ptrs);
	(function_type1(out_func_ptrs[0]))(123, NULL);

	typedef uint32_t(*function_type2) ();

	assert(133 == (function_type2(out_func_ptrs[1]))());

	ClangDestroy(cge);

	/*
	for (int i = 0; i < 100000; i++) {

	std::ifstream fs(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc.cpp");
	std::stringstream ss;
	ss << fs.rdbuf();

	const char * const function_names = "MatchUriForPort8181";
	void* out_functions[1];

	ClangCompileCodeAndGetFuntions(&cge, false, true, true, ss.str().c_str(), function_names, out_functions);

	std::ifstream fs2(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc2.cpp");
	std::stringstream ss2;
	ss2 << fs2.rdbuf();

	ClangCompileCodeAndGetFuntions(&cge, false, true, true, ss2.str().c_str(), function_names, out_functions);
	}*/

	return 0;
}