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

#include <sstream>
#include <cstdio>
#include <iostream>

class ParseOperation : public clang::ModuleLoader {
public:

    ParseOperation(
        const clang::LangOptions& options,
        clang::DiagnosticsEngine *engine,
        clang::PPCallbacks *callbacks = 0);

    virtual ~ParseOperation();

    clang::ASTContext * getASTContext() const;
    clang::Preprocessor * getPreprocessor() const;
    clang::SourceManager * getSourceManager() const;
    clang::TargetInfo * getTargetInfo() const;

    virtual clang::ModuleLoadResult loadModule(
        clang::SourceLocation ImportLoc,
        clang::ModuleIdPath Path,
        clang::Module::NameVisibilityKind Visibility,
        bool IsInclusionDirective);

    virtual void makeModuleVisible(
        clang::Module *Mod, 
        clang::Module::NameVisibilityKind NameVisibility,
        clang::SourceLocation ImportLoc)
    {}

private:

    clang::LangOptions lang_opts_;
    llvm::IntrusiveRefCntPtr<clang::TargetOptions> target_options_;
    llvm::IntrusiveRefCntPtr<clang::HeaderSearchOptions> hs_options_;
    llvm::IntrusiveRefCntPtr<clang::PreprocessorOptions> pp_options_;
    llvm::OwningPtr<clang::FileSystemOptions> fs_opts_;
    llvm::OwningPtr<clang::FileManager> fm_;
    llvm::OwningPtr<clang::SourceManager> sm_;
    llvm::OwningPtr<clang::HeaderSearch> hs_;
    llvm::OwningPtr<clang::Preprocessor> pp_;
    llvm::OwningPtr<clang::ASTContext> ast_;
    llvm::OwningPtr<clang::TargetInfo> target_;
};

ParseOperation::ParseOperation(
    const clang::LangOptions& options,
    clang::DiagnosticsEngine *diag,
    clang::PPCallbacks *callbacks) :
    lang_opts_(options),
    target_options_(new clang::TargetOptions),
    hs_options_(new clang::HeaderSearchOptions),
    pp_options_(new clang::PreprocessorOptions),
    fs_opts_(new clang::FileSystemOptions),
    fm_(new clang::FileManager(*fs_opts_)),
    sm_(new clang::SourceManager(*diag, *fm_))
{
    llvm::Triple triple(LLVM_DEFAULT_TARGET_TRIPLE);
    target_options_->ABI = "";
    target_options_->CPU = "";
    target_options_->Features.clear();
    target_options_->Triple = LLVM_DEFAULT_TARGET_TRIPLE;
    target_.reset(clang::TargetInfo::CreateTargetInfo(*diag, target_options_.getPtr()));
    hs_.reset(new clang::HeaderSearch(hs_options_, *fm_, *diag, options, &*target_));
    ApplyHeaderSearchOptions(*hs_, *hs_options_, options, triple);
    pp_.reset(new clang::Preprocessor(pp_options_, *diag, lang_opts_, &*target_, *sm_, *hs_, *this));
    pp_->addPPCallbacks(callbacks);
    clang::FrontendOptions frontendOptions;
    InitializePreprocessor(*pp_, *pp_options_, *hs_options_, frontendOptions);
    ast_.reset(
        new clang::ASTContext(lang_opts_,
        *sm_,
        &*target_,
        pp_->getIdentifierTable(),
        pp_->getSelectorTable(),
        pp_->getBuiltinInfo(),
        0));
}

ParseOperation::~ParseOperation()
{
}

clang::ASTContext * ParseOperation::getASTContext() const
{
    return ast_.get();
}

clang::Preprocessor * ParseOperation::getPreprocessor() const
{
    return pp_.get();
}

clang::SourceManager * ParseOperation::getSourceManager() const
{
    return sm_.get();
}

clang::TargetInfo * ParseOperation::getTargetInfo() const
{
    return target_.get();
}

clang::ModuleLoadResult ParseOperation::loadModule(
    clang::SourceLocation ImportLoc,
    clang::ModuleIdPath Path,
    clang::Module::NameVisibilityKind Visibility,
    bool IsInclusionDirective)
{
    return clang::ModuleLoadResult();
}

class Parser {
public:

    explicit Parser(const clang::LangOptions& options);
    ~Parser();

    enum InputType { Incomplete, TopLevel, Stmt }; 

    // Analyze the specified input to determine whether its complete or not.
    InputType analyzeInput(const std::string& contextSource,
        const std::string& buffer,
        int& indentLevel,
        std::vector<clang::FunctionDecl*> *fds);

    // Create a new ParseOperation that the caller should take ownership of
    // and the lifetime of which must be shorter than of the Parser.
    ParseOperation * CreateParseOperation(clang::DiagnosticsEngine *engine,
        clang::PPCallbacks *callbacks = 0);

    // Parse the specified source code with the specified parse operation
    // and consumer. Upon parsing, ownership of parseOp is transferred to
    // the Parser permanently.
    void Parse(const std::string& src,
        ParseOperation *parseOp,
        clang::ASTConsumer *consumer);

    // Parse by implicitly creating a ParseOperation. Equivalent to
    // parse(src, createParseOperation(diag), consumer).
    void Parse(const std::string& src,
        clang::DiagnosticsEngine *engine,
        clang::ASTConsumer *consumer);

    // Returns the last parse operation or NULL if there isn't one.
    ParseOperation * getLastParseOperation() const;

    // Release any accumulated parse operations (including their resulting
    // ASTs and other clang data structures).
    void releaseAccumulatedParseOperations();
private:

    const clang::LangOptions& options_;
    std::vector<ParseOperation*> ops_;

    static llvm::MemoryBuffer * createMemoryBuffer(
        const std::string& src,
        const char *name,
        clang::SourceManager *sm);
};

Parser::Parser(const clang::LangOptions& options) : options_(options)
{
}

void Parser::Parse(
    const std::string& src,
    ParseOperation *parseOp,
    clang::ASTConsumer *consumer)
{
    ops_.push_back(parseOp);
    createMemoryBuffer(src, "", parseOp->getSourceManager());
    clang::ParseAST(*parseOp->getPreprocessor(), consumer, *parseOp->getASTContext());
}

void Parser::Parse(
    const std::string& src,
    clang::DiagnosticsEngine *engine,
    clang::ASTConsumer *consumer)
{
    Parse(src, CreateParseOperation(engine), consumer);
}

llvm::MemoryBuffer * Parser::createMemoryBuffer(
    const std::string& src,
    const char *name,
    clang::SourceManager *sm)
{
    llvm::MemoryBuffer *mb = llvm::MemoryBuffer::getMemBufferCopy(src, name);
    assert(mb && "Error creating MemoryBuffer!");

    sm->createMainFileIDForMemBuffer(mb);
    assert(!sm->getMainFileID().isInvalid() && "Error creating MainFileID!");

    return mb;
}

Parser::~Parser()
{
    releaseAccumulatedParseOperations();
}

void Parser::releaseAccumulatedParseOperations()
{
    for (std::vector<ParseOperation*>::iterator I = ops_.begin(), E = ops_.end(); I != E; ++I)
        delete *I;

    ops_.clear();
}

ParseOperation * Parser::CreateParseOperation(
    clang::DiagnosticsEngine *engine,
    clang::PPCallbacks *callbacks)
{
    return new ParseOperation(options_, engine, callbacks);
}

#include <time.h>

class CodegenEngine
{
    clang::CodeGenOptions code_gen_options_;
    clang::TargetOptions target_options_;
    clang::LangOptions lang_options_;
    clang::DiagnosticOptions diag_options_;
    llvm::IntrusiveRefCntPtr<clang::DiagnosticIDs> diag_ids_;

    llvm::LLVMContext* llvm_context_;
    clang::CodeGenerator* codegen_;

    Parser* parser_;
    llvm::ExecutionEngine* exec_engine_;
    llvm::Linker* linker_;
    clang::DiagnosticsEngine* diag_engine_;

    //clang::TextDiagnosticPrinter* tdp_;
    clang::TextDiagnosticBuffer* tdp_;

    // Currently used module.
    llvm::Module *module_;
    ParseOperation *parse_op_;

public:

    CodegenEngine()
    {
        diag_ids_ = NULL;
        llvm_context_ = NULL;
        codegen_ = NULL;
        parser_ = NULL;
        exec_engine_ = NULL;
        linker_ = NULL;
        diag_engine_ = NULL;

        tdp_ = NULL;
        module_ = NULL;
        parse_op_ = NULL;

        lang_options_.CPlusPlus = true;
        lang_options_.Bool = true;
        lang_options_.Optimize = 1;

        parser_ = new Parser(lang_options_);

        code_gen_options_.InstrumentFunctions = false;

        diag_ids_ = new clang::DiagnosticIDs(); 

        tdp_ = new clang::TextDiagnosticBuffer(); //new clang::TextDiagnosticPrinter(llvm::errs(), &g_diag_options);
        diag_engine_ = new clang::DiagnosticsEngine(diag_ids_, &diag_options_, tdp_);

        llvm_context_ = new llvm::LLVMContext();

        // Creating new linker.
        linker_ = new llvm::Linker("sccodegen", "sccodegen", *llvm_context_);
    }

    void Cleanup(bool accumulate_old_modules)
    {
        if (exec_engine_)
            exec_engine_->removeModule(module_);

        if (!accumulate_old_modules)
        {
            delete module_;
            module_ = NULL;
        }

        delete exec_engine_;
        exec_engine_ = NULL;

        delete codegen_;
        codegen_ = NULL;

        delete parse_op_;
        parse_op_ = NULL;
    }

    void* CompileCodeAndGetFuntion(
        const char* code_str,
        const char* func_name,
        bool accumulate_old_modules)
    {
        clock_t start = clock();

        std::string src = code_str, fname = func_name, error_str;
        void* fp = NULL;

        // Performing cleanup before the new round.
        if (module_)
            Cleanup(accumulate_old_modules);

        // Resetting diagnostics engine.
        tdp_->clear();
        diag_engine_->Reset();
        codegen_ = CreateLLVMCodeGen(*diag_engine_, "-", code_gen_options_, target_options_, *llvm_context_);

        // Creating parsing engines.
        parse_op_ = parser_->CreateParseOperation(diag_engine_);
        tdp_->BeginSourceFile(lang_options_, parse_op_->getPreprocessor());

        // Parsing the input source file.
        parser_->Parse(src, parse_op_, codegen_);
        if (diag_engine_->hasErrorOccurred())
            return NULL;

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
        llvm::Function *module_func = module_->getFunction(fname.c_str());
        assert(module_func != NULL);

        // Obtaining the pointer to created function.
        fp = exec_engine_->getPointerToFunction(module_func);

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
    clang_engine->Cleanup(false);
}

#include <fstream>

int32_t main()
{
    GwClangInit();

    CodegenEngine* cge1 = NULL;
    CodegenEngine* cge2 = NULL;

    int sum = 0;
    typedef int (*pmain) ();
    pmain pp;

    struct ParamInfo {
        uint16_t offset;
        uint16_t len;
    };

    typedef int (*pmatch) (char* puri, int32_t size, ParamInfo** params);

    std::ifstream config_file_stream(L"c:\\github\\Level1\\bin\\Debug\\.srv\\Personal\\Logs\\scnetworkgateway\\rps\\codegen_uri_matcher_11112.cpp");
    std::stringstream str_stream;
    str_stream << config_file_stream.rdbuf();
    
    std::ifstream config_file_stream2(L"c:\\github\\Level1\\bin\\Debug\\.srv\\Personal\\Logs\\scnetworkgateway\\rps\\codegen_uri_matcher_8181.cpp");
    std::stringstream str_stream2;
    str_stream2 << config_file_stream2.rdbuf();

    for (int i = 0; i < 1000; i++)
    {
        std::cout << i << std::endl;

        pmatch pm = (pmatch)GwClangCompileCodeAndGetFuntion(&cge1, str_stream.str().c_str(), "MatchUriForPort11112", false);
        pm("GET / ", 6, NULL);

        pm = (pmatch)GwClangCompileCodeAndGetFuntion(&cge2, str_stream2.str().c_str(), "MatchUriForPort8181", false);
        pm("GET / ", 6, NULL);
    }

    GwClangDestroyEngine(cge1);
    GwClangDestroyEngine(cge2);
    cge1 = NULL;
    cge2 = NULL;

    return 0;

    if (pp)
    {
        for (int i = 0; i < 1000000; i++)
            sum += pp();
        std::cout << "Total sum: " << sum << std::endl;
    }

    std::ifstream config_file_stream3(L"c:\\github\\Level1\\bin\\Debug\\.db.output\\codegen\\700400.cpp");
    std::stringstream str_stream3;
    str_stream2 << config_file_stream3.rdbuf();

    pp = (pmain)GwClangCompileCodeAndGetFuntion(&cge1, str_stream2.str().c_str(), "_ValidateKey_7341056", false);
    return 0;

    if (pp)
    {
        for (int i = 0; i < 1000000; i++)
            sum += pp();
        std::cout << "Total sum: " << sum << std::endl;
    }

    pp = (pmain)GwClangCompileCodeAndGetFuntion(&cge1, "int main() { return 125; }", "main", false);

    if (pp)
    {
        for (int i = 0; i < 1000000; i++)
            sum += pp();
        std::cout << "Total sum: " << sum << std::endl;
    }

    GwClangShutdown();

    // llvm::Function *ext_func = g_module->getFunction("print_me");
    // g_exec_engine->addGlobalMapping(ext_func, &print_me);
}

//===----------------------------------------------------------------------===//
// Main driver
//===----------------------------------------------------------------------===//
/*
static void LLVMErrorHandler(void *UserData, const std::string &Message) {
  DiagnosticsEngine &Diags = *static_cast<DiagnosticsEngine*>(UserData);

  Diags.Report(diag::err_fe_error_backend) << Message;

  // Run the interrupt handlers to make sure any special cleanups get done, in
  // particular that we remove files registered with RemoveFileOnSignal.
  llvm::sys::RunInterruptHandlers();

  // We cannot recover from llvm errors.  When reporting a fatal error, exit
  // with status 70.  For BSD systems this is defined as an internal software
  // error.  This notifies the driver to report diagnostics information.
  exit(70);
}

int cc1_main(const char **ArgBegin, const char **ArgEnd,
             const char *Argv0, void *MainAddr) {
  OwningPtr<CompilerInstance> Clang(new CompilerInstance());
  IntrusiveRefCntPtr<DiagnosticIDs> DiagID(new DiagnosticIDs());

  // Initialize targets first, so that --version shows registered targets.
  llvm::InitializeAllTargets();
  llvm::InitializeAllTargetMCs();
  llvm::InitializeAllAsmPrinters();
  llvm::InitializeAllAsmParsers();

  // Buffer diagnostics from argument parsing so that we can output them using a
  // well formed diagnostic object.
  IntrusiveRefCntPtr<DiagnosticOptions> DiagOpts = new DiagnosticOptions();
  TextDiagnosticBuffer *DiagsBuffer = new TextDiagnosticBuffer;
  DiagnosticsEngine Diags(DiagID, &*DiagOpts, DiagsBuffer);
  bool Success;
  Success = CompilerInvocation::CreateFromArgs(Clang->getInvocation(),
                                               ArgBegin, ArgEnd, Diags);

  // Infer the builtin include path if unspecified.
  if (Clang->getHeaderSearchOpts().UseBuiltinIncludes &&
      Clang->getHeaderSearchOpts().ResourceDir.empty())
    Clang->getHeaderSearchOpts().ResourceDir =
      CompilerInvocation::GetResourcesPath(Argv0, MainAddr);

  // Create the actual diagnostics engine.
  Clang->createDiagnostics();
  if (!Clang->hasDiagnostics())
    return 1;

  // Set an error handler, so that any LLVM backend diagnostics go through our
  // error handler.
  llvm::install_fatal_error_handler(LLVMErrorHandler,
                                  static_cast<void*>(&Clang->getDiagnostics()));

  DiagsBuffer->FlushDiagnostics(Clang->getDiagnostics());
  if (!Success)
    return 1;

  // Execute the frontend actions.
  Success = ExecuteCompilerInvocation(Clang.get());

  // If any timers were active but haven't been destroyed yet, print their
  // results now.  This happens in -disable-free mode.
  llvm::TimerGroup::printAll(llvm::errs());

  // Our error handler depends on the Diagnostics object, which we're
  // potentially about to delete. Uninstall the handler now so that any
  // later errors use the default handling behavior instead.
  llvm::remove_fatal_error_handler();

  // When running with -disable-free, don't do any destruction or shutdown.
  if (Clang->getFrontendOpts().DisableFree) {
    if (llvm::AreStatisticsEnabled() || Clang->getFrontendOpts().ShowStats)
      llvm::PrintStatistics();
    Clang.take();
    return !Success;
  }

  // Managed static deconstruction. Useful for making things like
  // -time-passes usable.
  llvm::llvm_shutdown();

  return !Success;
}
*/