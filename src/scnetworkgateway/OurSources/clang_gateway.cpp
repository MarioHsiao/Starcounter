#include "clang/Driver/Arg.h"
#include "clang/Driver/ArgList.h"
#include "clang/Driver/DriverDiagnostic.h"
#include "clang/Driver/OptTable.h"
#include "clang/Driver/Options.h"
#include "clang/Frontend/CompilerInstance.h"
#include "clang/Frontend/CompilerInvocation.h"
#include "clang/Frontend/FrontendDiagnostic.h"
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/FrontendTool/Utils.h"
#include "clang/CodeGen/ModuleBuilder.h"
#include "llvm/ADT/Statistic.h"
#include "llvm/LinkAllPasses.h"
#include "llvm/Support/ErrorHandling.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Support/Signals.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/Support/Timer.h"
#include "llvm/Support/raw_ostream.h"
#include "llvm/Support/raw_os_ostream.h"
#include "llvm/ADT/OwningPtr.h"
#include "llvm/Support/MemoryBuffer.h"

#include "clang/Basic/FileManager.h"
#include "clang/Basic/LangOptions.h"
#include "clang/Basic/TargetOptions.h"
#include "clang/Lex/HeaderSearchOptions.h"
#include "clang/Lex/PreprocessorOptions.h"
#include "clang/Lex/HeaderSearch.h"
#include "clang/Lex/ModuleLoader.h"

#include "llvm/ADT/OwningPtr.h"
#include "llvm/Support/MemoryBuffer.h"
#include "llvm/Linker.h"
#include "llvm/ExecutionEngine/ExecutionEngine.h"
#include "llvm/ExecutionEngine/GenericValue.h"
#include "llvm/ExecutionEngine/JIT.h"

#include "clang/AST/AST.h"
#include "clang/Basic/LangOptions.h"
#include "clang/Basic/SourceManager.h"
#include "clang/Basic/TargetInfo.h"
#include "clang/CodeGen/ModuleBuilder.h"
#include "clang/Frontend/CodeGenOptions.h"
#include "clang/Lex/Preprocessor.h"
#include "clang/Lex/MacroInfo.h"
#include "llvm/IR/Module.h"
#include "clang/Sema/SemaDiagnostic.h"
#include "clang/Lex/LexDiagnostic.h"
#include "clang/Frontend/FrontendOptions.h"
#include "clang/Frontend/Utils.h"
#include "clang/Parse/ParseAST.h"
#include "llvm\Support\TargetSelect.h"

#include "llvm/Support/Host.h"
#include "llvm/ADT/IntrusiveRefCntPtr.h"

#include "clang/Basic/DiagnosticOptions.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/Frontend/CompilerInstance.h"
#include "clang/Basic/TargetOptions.h"
#include "clang/Basic/TargetInfo.h"
#include "clang/Basic/LangOptions.h"
#include "clang/Basic/FileManager.h"
#include "clang/Basic/SourceManager.h"
#include "clang/Lex/Preprocessor.h"
#include "clang/Basic/Diagnostic.h"
#include "clang/AST/ASTContext.h"
#include "clang/AST/ASTConsumer.h"
#include "clang/Basic/LangOptions.h"
#include "clang/Parse/Parser.h"
#include "clang/Parse/ParseAST.h"
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Frontend/TextDiagnosticPrinter.h"
#include "clang/Basic/llvm.h"

#include <sstream>
#include <cstdio>
#include <iostream>
#include <time.h>
#include <fstream>

class CodegenEngine
{
    llvm::LLVMContext* llvm_context_;

    llvm::ExecutionEngine* exec_engine_;
    llvm::Linker* linker_;

    llvm::Module *module_;

public:

    ~CodegenEngine()
    {
        delete exec_engine_;
    }

    CodegenEngine()
    {
        exec_engine_ = NULL;
        linker_ = NULL;
        module_ = NULL;

        llvm_context_ = new llvm::LLVMContext();

        // Creating new linker.
        linker_ = new llvm::Linker("sccodegen", "sccodegen", *llvm_context_);
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
    }

    void* CompileCodeAndGetFuntion(
        const char* code_str,
        const char* func_name,
        bool accumulate_old_modules)
    {
        clock_t start = clock();

        std::string code_string = code_str, func_name_string = func_name, error_str;

        // Performing cleanup before the new round.
        if (module_)
            Cleanup(accumulate_old_modules);

        clang::CompilerInstance ci;
        clang::CodeGenOptions code_gen_options;
        code_gen_options.OptimizationLevel = 3; // All optimizations.

        clang::TargetOptions* target_options = new clang::TargetOptions();
        target_options->Triple = llvm::sys::getDefaultTargetTriple();

        llvm::IntrusiveRefCntPtr<clang::DiagnosticOptions> diagnostic_options = new clang::DiagnosticOptions();
        clang::DiagnosticConsumer* diagnostic_client = new clang::TextDiagnosticBuffer();
        //new clang::TextDiagnosticPrinter(llvm::errs(), &*diagnostic_options);

        llvm::IntrusiveRefCntPtr<clang::DiagnosticIDs> diagnostic_id(new clang::DiagnosticIDs());
        llvm::IntrusiveRefCntPtr<clang::DiagnosticsEngine> diagnostic_engine = 
            new clang::DiagnosticsEngine(diagnostic_id, &*diagnostic_options, &*diagnostic_client);

        clang::CodeGenerator* codegen_ = CreateLLVMCodeGen(*diagnostic_engine, "-", code_gen_options, *target_options, *llvm_context_);

        ci.createDiagnostics(&*diagnostic_client);

        clang::TargetInfo *pti = clang::TargetInfo::CreateTargetInfo(ci.getDiagnostics(), target_options);
        ci.setTarget(pti);
        ci.setDiagnostics(&*diagnostic_engine);

        clang::LangOptions& lang_options = ci.getLangOpts();
        lang_options.GNUMode = 1; 
        lang_options.CXXExceptions = 1; 
        lang_options.RTTI = 1; 
        lang_options.Bool = 1; 
        lang_options.CPlusPlus = 1; 
        lang_options.Optimize = 1;

        ci.getCodeGenOpts() = code_gen_options;
        ci.createFileManager();
        ci.createSourceManager(ci.getFileManager());
        ci.createPreprocessor();
        ci.getPreprocessorOpts().UsePredefines = false;
        clang::ASTConsumer *astConsumer = new clang::ASTConsumer();
        ci.setASTConsumer(astConsumer);

        ci.createASTContext();
        ci.createSema(clang::TU_Complete, NULL);

        llvm::MemoryBuffer *mb = llvm::MemoryBuffer::getMemBufferCopy(code_string, "some");
        assert(mb && "Error creating MemoryBuffer!");

        ci.getSourceManager().createMainFileIDForMemBuffer(mb);
        diagnostic_client->BeginSourceFile(lang_options);
        //clang::ParseAST(ci.getSema());
        clang::ParseAST(ci.getPreprocessor(), codegen_, ci.getASTContext());
        //ci.getASTContext().Idents.PrintStats();

        // Creating new module.
        module_ = codegen_->ReleaseModule();
        assert(module_ != NULL);

        // Linking all old modules together.
        if (accumulate_old_modules)
        {
            // Linking the new module.
            linker_->LinkInModule(module_, &error_str);
            if (!error_str.empty())
            {
                std::cout << "Linking problems: " << error_str << std::endl;
                return NULL;
            }

            // Link module with the existing ones.
            module_ = linker_->getModule();
        }

        // Creating new execution engine for this module.
        exec_engine_ = llvm::ExecutionEngine::create(module_, false, &error_str, llvm::CodeGenOpt::Aggressive);
        assert(exec_engine_ != NULL);

        llvm::Function* module_func = module_->getFunction(func_name_string.c_str());
        assert(module_func != NULL);

        // Obtaining the pointer to created function.
        void* fp = exec_engine_->getPointerToFunction(module_func);

        delete codegen_;
        codegen_ = NULL;

        clock_t end = clock();
        float seconds = (float)(end - start) / CLOCKS_PER_SEC;

        std::cout << "Clang took seconds: " << seconds << std::endl;

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
    assert(clang_engine != NULL);

    clang_engine->Cleanup(false);

    delete clang_engine;
}

int main()
{
    GwClangInit();

    CodegenEngine* cge = NULL; 

    std::ifstream fs(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc.cpp");
    std::stringstream ss;
    ss << fs.rdbuf();

    GwClangCompileCodeAndGetFuntion(&cge, ss.str().c_str(), "MatchUriForPort8181", false);

    std::ifstream fs2(L"C:\\Users\\Alexey Moiseenko\\Desktop\\ccc2.cpp");
    std::stringstream ss2;
    ss2 << fs2.rdbuf();

    GwClangCompileCodeAndGetFuntion(&cge, ss2.str().c_str(), "MatchUriForPort8181", false);

    return 0;
}