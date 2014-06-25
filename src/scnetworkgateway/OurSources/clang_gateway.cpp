#include "clang/CodeGen/CodeGenAction.h"
#include "clang/Basic/DiagnosticOptions.h"
#include "clang/Basic/TargetInfo.h"
#include "clang/Driver/Compilation.h"
#include "clang/Driver/Driver.h"
#include "clang/Driver/Tool.h"
#include "clang/Frontend/CompilerInstance.h"
#include "clang/Frontend/CompilerInvocation.h"
#include "clang/Frontend/FrontendDiagnostic.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Parse/ParseAST.h"
#include "llvm/ADT/SmallString.h"
#include "llvm/ExecutionEngine/ExecutionEngine.h"
#include "llvm/ExecutionEngine/JIT.h"
#include "llvm/IR/Module.h"
#include "llvm/Support/FileSystem.h"
#include "llvm/Support/Host.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Support/Path.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/Support/raw_ostream.h"
#include "llvm/IR/LLVMContext.h"
#include "clang/codegen/ModuleBuilder.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>

class CodegenEngine
{
    llvm::ExecutionEngine* exec_engine_;
    llvm::Module *module_;
    llvm::LLVMContext* llvm_context_;

public:

    ~CodegenEngine()
    {

    }

    CodegenEngine()
    {
        exec_engine_ = NULL;
        module_ = NULL;
        llvm_context_ = new llvm::LLVMContext();
    }

    void Cleanup(bool accumulate_old_modules)
    {
        if (exec_engine_)
            exec_engine_->removeModule(module_);

        if (!accumulate_old_modules) {
            delete module_;
            module_ = NULL;
        }

        if (exec_engine_) {
            delete exec_engine_;
            exec_engine_ = NULL;
        }

        delete llvm_context_;
        llvm_context_ = new llvm::LLVMContext();
    }

    void* CompileCodeAndGetFuntion(
        const char* code_str,
        const char* func_name,
        bool accumulate_old_modules)
    {
        using namespace clang;
        using namespace llvm;

        bool optimize = true;

        clock_t start_parsing = clock();

        std::string code_string = code_str, func_name_string = func_name, error_str;

        // Performing cleanup before the new round.
        if (module_)
            Cleanup(accumulate_old_modules);

        CompilerInstance ci;
        CodeGenOptions code_gen_options;
        code_gen_options.DisableFree = 0;

        if (optimize) {
            code_gen_options.OptimizationLevel = 3; // All optimizations.
        } else {
            code_gen_options.OptimizationLevel = 0; // No optimizations.
            code_gen_options.OptimizeSize = 0;
            code_gen_options.NoInline = 1;
        }

        clang::TargetOptions* target_options = new clang::TargetOptions();
        target_options->Triple = sys::getDefaultTargetTriple();

        IntrusiveRefCntPtr<DiagnosticOptions> diagnostic_options = new DiagnosticOptions();
        DiagnosticConsumer* diagnostic_client = new TextDiagnosticBuffer();
            //new TextDiagnosticPrinter(errs(), &*diagnostic_options);

        IntrusiveRefCntPtr<DiagnosticIDs> diagnostic_id(new DiagnosticIDs());
        IntrusiveRefCntPtr<DiagnosticsEngine> diagnostic_engine = 
            new DiagnosticsEngine(diagnostic_id, &*diagnostic_options, &*diagnostic_client);

        CodeGenerator* codegen_ = CreateLLVMCodeGen(*diagnostic_engine, "-", code_gen_options, *target_options, *llvm_context_);

        ci.setDiagnostics(&*diagnostic_engine);

        TargetInfo *pti = TargetInfo::CreateTargetInfo(ci.getDiagnostics(), target_options);
        ci.setTarget(pti);

        LangOptions& lang_options = ci.getLangOpts();
        lang_options.GNUMode = 1; 
        lang_options.CXXExceptions = 1; 
        lang_options.RTTI = 1; 
        lang_options.Bool = 1; 
        lang_options.CPlusPlus = 1;

        if (optimize) {
            lang_options.Optimize = 1;
        } else {
            lang_options.Optimize = 0;
        }

        ci.getCodeGenOpts() = code_gen_options;
        ci.createFileManager();
        ci.createSourceManager(ci.getFileManager());
        ci.createPreprocessor();
        ci.getPreprocessorOpts().UsePredefines = false;
        ci.getFrontendOpts().DisableFree = 0;
        ci.getDiagnostics().setIgnoreAllWarnings(true);
        ci.getDiagnosticOpts().IgnoreWarnings = 1;

        MemoryBuffer *mb = MemoryBuffer::getMemBufferCopy(code_string, "some");
        assert(mb && "Error creating MemoryBuffer!");

        ci.setASTConsumer(codegen_);
        ci.createASTContext();

        ci.getSourceManager().createMainFileIDForMemBuffer(mb);
        ci.getDiagnosticClient().BeginSourceFile(lang_options);
        ParseAST(ci.getPreprocessor(), codegen_, ci.getASTContext());
        ci.getDiagnosticClient().EndSourceFile();
        
        clock_t end_parsing = clock();

        clock_t start_jiting = clock();

        // Creating new module.
        module_ = codegen_->ReleaseModule();
        assert(module_ && "Can't release module by some reason!");

        // Creating new execution engine for this module.
        if (optimize) {
            exec_engine_ = ExecutionEngine::create(module_, false, &error_str, CodeGenOpt::Aggressive);
        } else {
            exec_engine_ = ExecutionEngine::create(module_, false, &error_str, CodeGenOpt::None);
        }
        assert(exec_engine_ && "Can't create execution engine by some reason!");

        Function* module_func = module_->getFunction(func_name_string.c_str());
        assert(module_func && "Can't find function from generated code module!");

        // Obtaining the pointer to created function.
        void* fp = exec_engine_->getPointerToFunction(module_func);
        assert(fp && "Can't get function address from JITed code!");

        clock_t end_jiting = clock();
        
        float seconds_parsing = (float) (end_parsing - start_parsing) / CLOCKS_PER_SEC,
            seconds_jiting = (float) (end_jiting - start_jiting) / CLOCKS_PER_SEC;

        std::cout << "Codegen took seconds: " << seconds_parsing + seconds_jiting << " (" << seconds_parsing << ", " << seconds_jiting << ")." << std::endl;

        return fp;
    }
};

extern "C" __declspec(dllexport) void GwClangInit()
{
    llvm::InitializeNativeTarget();
}

extern "C" __declspec(dllexport) void GwClangShutdown()
{
    llvm::llvm_shutdown();
}

extern "C" __declspec(dllexport) void* GwClangCompileCodeAndGetFuntion(
    CodegenEngine** cge,
    const char* code_str,
    const char* func_name,
    bool accumulate_old_modules)
{
    if (NULL == *cge)
        *cge = new CodegenEngine();

    return (*cge)->CompileCodeAndGetFuntion(code_str, func_name, accumulate_old_modules);
}

extern "C" __declspec(dllexport) void GwClangDestroyEngine(CodegenEngine* clang_engine)
{
    assert(clang_engine && "Engine must exist to be destroyed!");

    clang_engine->Cleanup(false);

    delete clang_engine;
}

int main()
{
    GwClangInit();

    CodegenEngine* cge = NULL; 

    for (int i = 0; i < 100000; i++) {

        std::ifstream fs(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc.cpp");
        std::stringstream ss;
        ss << fs.rdbuf();

        GwClangCompileCodeAndGetFuntion(&cge, ss.str().c_str(), "MatchUriForPort8181", false);

        std::ifstream fs2(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc2.cpp");
        std::stringstream ss2;
        ss2 << fs2.rdbuf();

        GwClangCompileCodeAndGetFuntion(&cge, ss2.str().c_str(), "MatchUriForPort8181", false);
    }

    return 0;
}