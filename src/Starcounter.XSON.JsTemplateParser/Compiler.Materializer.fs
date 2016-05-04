// Starcounter Internal Template Ecmascript Parser
// Copyright (c) Starcounter AB


namespace Starcounter.Internal.JsonTemplate
open System;

type ITemplate =
    interface
    end
   

type IObjectTemplate =
    interface
    end


// Interface declaration:
type ITemplateFactory =
    interface
        abstract AddAppProperty : obj * string * string * DebugInfo -> obj               // Name
        abstract AddTString : obj * string * string * string * DebugInfo -> obj        // Name, value
        abstract AddIntegerProperty : obj * string * string * int * DebugInfo -> obj        // Name, value
        abstract AddTDecimal : obj * string * string * decimal * DebugInfo -> obj        // Name, value
        abstract AddTDouble : obj * string * string * double * DebugInfo -> obj        // Name, value
        abstract AddBooleanProperty : obj * string * string * bool * DebugInfo -> obj        // Name, value
        abstract AddEventProperty : obj * string * string * string * DebugInfo -> obj        // Name, value
        abstract AddArrayProperty : obj * string * string * DebugInfo -> obj                // Name
        abstract AddCargoProperty : obj * DebugInfo -> obj         
        abstract AddMetaProperty : obj * DebugInfo -> obj         
        abstract GetMetaTemplate : obj * DebugInfo -> obj
        abstract GetMetaTemplateForProperty : obj * string * DebugInfo -> obj
        abstract SetEditableProperty : obj * bool * DebugInfo -> unit
        abstract SetClassProperty : obj * string * DebugInfo -> unit
        abstract SetIncludeProperty : obj * string * DebugInfo -> unit
        abstract SetNamespaceProperty : obj * string * DebugInfo -> unit
        abstract SetOnUpdateProperty : obj * string * DebugInfo -> unit
        abstract SetBindProperty : obj * string * DebugInfo -> unit
    end


module public Materializer =
    let AstToString (ast:obj) = // JOCKE
        Parser.parse ("test","test",1 ) |> ignore
        sprintf "%A" ast

    let Parse (source:Src) =
        source |> Parser.parse

    let BuiltTemplate( source:string, sourceReference:string, overhead:int, factory:ITemplateFactory, ignoreNonDesignTimeAssignments:bool ) : obj =

        let astTree, scopeData =
            match ignoreNonDesignTimeAssignments with
            | true ->  FullParser.parse ( source, sourceReference, overhead )
            | false -> Parser.parse ( source, sourceReference, overhead )

        let printa text indentation = 
            for i in 1 .. indentation do
                printf " "
            printfn "%s" text

        let failedExpectation1 expected found =
            failwith (sprintf "Expected %A but found a %A" expected found )

        let failedExpectation notEmbedded expected found =
            if notEmbedded then
                null
            else
                failwith (sprintf "Expected %A but found a %A" expected found )

                
        let attachMetadataFromInvoke (factory:ITemplateFactory) (parent:obj) (parentAst:Ast.Tree) (expr:Ast.Tree) (identifier:string) (parameters:Ast.Tree list) =
            match identifier with
            | "Class" ->
                let firstParam = Seq.head parameters
                match firstParam with
                | Ast.Tree.String (str,debugInfo) ->
                    factory.SetClassProperty( parent, str, debugInfo )
                | _ -> failedExpectation1 ".Class() adorner function takes a string with the name of the .NET class to use" firstParam
            | "Include" ->
                let firstParam = Seq.head parameters
                match firstParam with
                | Ast.Tree.String (str,debugInfo) ->
                    factory.SetIncludeProperty( parent, str, debugInfo )
                | _ -> failedExpectation1 ".Include() adorner function takes a string with the name of the .NET class to use" firstParam
            | "Namespace" ->
                let firstParam = Seq.head parameters
                match firstParam with
                | Ast.Tree.String (str,debugInfo) ->
                    factory.SetNamespaceProperty( parent, str, debugInfo )
                | _ -> failedExpectation1 ".Namespace() adorner function takes a string with the name of the .NET namespace to use" firstParam
            | "OnUpdate" ->
                let firstParam = Seq.head parameters
                match firstParam with
                | Ast.Tree.String (str,debugInfo) ->
                    factory.SetOnUpdateProperty( parent, str, debugInfo )
                | _ -> failedExpectation1 ".OnUpdate() adorner function takes a string with the name of the .NET namespace to use" firstParam
            | "Editable" ->
                if (Seq.isEmpty parameters) then
                    match parentAst with
                    | Ast.Tree.Invoke (_,_,debugInfo) ->
                       factory.SetEditableProperty( parent, true, debugInfo )
                    | _ -> failedExpectation1 "Invoke" expr
                else
                    let firstParam = Seq.head parameters
                    match firstParam with
                    | Ast.Tree.Boolean (b,debugInfo) ->
                        factory.SetEditableProperty( parent, b, debugInfo )
                    | _ -> failedExpectation1 ".Editable() adorner takes a boolean or no parameter." firstParam
            | "Bind" ->
                let firstParam = Seq.head parameters
                match firstParam with
                | Ast.Tree.String (str,debugInfo) ->
                    factory.SetBindProperty( parent, str, debugInfo )
                | _ -> failedExpectation1 ".Bind() adorner function takes a string with the property path to bind to the .Net property in the Starcounter Entity object" firstParam
            | "Unbound" ->
                if (Seq.isEmpty parameters) then
                    match parentAst with
                    | _ -> failedExpectation1 "Invoke" expr
                else
                    failedExpectation1 ".Unbound() adorner function does not take any parameters." parameters
            | _ -> failedExpectation1 "Adorner function Class, Include, Namespace, Editable, Bind, Unbound or OnUpdate" identifier

        let createTemplate (parent:obj) (name:string) (ast:Ast.Tree) (factory:ITemplateFactory)  =
            let dollarPrefix = if (name = null) then 
                                   false
                               else
                                   (name.Chars(0) = '$')
            let dollarSuffix = if (name = null) then 
                                   false
                               else
                                   (name.Chars(name.Length-1) = '$')
            let legalName = if (dollarPrefix) then name.Substring(1) else name;
            if dollarPrefix then
                match ast with // TODO: Can we assume that metadata is always treated as an Ast.Object?
                | Ast.Object (_, debugInfo) ->
                    if ( legalName = "" ) then
                        factory.GetMetaTemplate(parent, debugInfo)
                    else
                        factory.GetMetaTemplateForProperty(parent, legalName, debugInfo)
                | _ ->
                    failwith "Unexpected metadata."
            else
                let legalName = if (dollarSuffix) then 
                                    legalName.Substring(0, legalName.Length - 1) 
                                 else 
                                    legalName

                // Consume an eventual negative token before matching and creating a template.
                let (realtree, valueIsMinus) = match ast with 
                                               | Ast.Tree.Unary (op, tree) when op = Ast.UnaryOp.Minus ->
                                                  tree, true
                                               | _ ->
                                                  ast, false

                let ( newObj, debugInfo, considerEditable ) =
                        match realtree with
                        | Ast.String (str,debugInfo) ->
                            ( factory.AddTString(parent,name,legalName,str,debugInfo), debugInfo, true )
                        | Ast.Boolean(b,debugInfo) ->
                            ( factory.AddBooleanProperty(parent,name,legalName,b,debugInfo), debugInfo, true )
                        | Ast.Identifier(identifier,debugInfo) ->
                            ( factory.AddEventProperty(parent,name,legalName,identifier,debugInfo), debugInfo, false )
                        | Ast.Function(_,_,_,debugInfo) ->
                            ( factory.AddEventProperty(parent,name,legalName,null,debugInfo), debugInfo, false )
                        | Ast.Tree.Array (_,debugInfo) ->
                            ( factory.AddArrayProperty(parent,name,legalName,debugInfo), debugInfo, false )
                        | Ast.Tree.Object (_,debugInfo) ->
                            (factory.AddAppProperty(parent,name,legalName,debugInfo), debugInfo, true )
                        | Ast.Tree.Null (debugInfo) ->
                            ( factory.AddEventProperty(parent,name,legalName,null,debugInfo), debugInfo, true )
   //                         (factory.AddObjectProperty(parent,legalName,debugInfo), debugInfo, false )
                        | Ast.Tree.Integer (i,debugInfo) when valueIsMinus ->
                            (factory.AddIntegerProperty(parent,name,legalName,-i,debugInfo), debugInfo, true )
                        | Ast.Tree.Integer (i,debugInfo) ->
                            (factory.AddIntegerProperty(parent,name,legalName,i,debugInfo), debugInfo, true )
                        | Ast.Tree.Decimal (d, debugInfo) when valueIsMinus ->
                            (factory.AddTDecimal(parent,name,legalName,-d,debugInfo), debugInfo, true )
                        | Ast.Tree.Decimal (d, debugInfo) ->
                            (factory.AddTDecimal(parent,name,legalName,d,debugInfo), debugInfo, true )
                        | Ast.Tree.Double (d, debugInfo) when valueIsMinus ->
                            (factory.AddTDouble(parent,name,legalName,-d,debugInfo), debugInfo, true )
                        | Ast.Tree.Double (d, debugInfo) ->
                            (factory.AddTDouble(parent,name,legalName,d,debugInfo), debugInfo, true )
                        | _ ->
                            failedExpectation1 "array, object, string, boolean, number, function or event" realtree
                if ( considerEditable && dollarSuffix ) then factory.SetEditableProperty( newObj, true, debugInfo );
                newObj

        let rec materializePropertyOfParent (parent:obj) (parentAst:Ast.Tree) (name:string) (ast:Ast.Tree) (factory:ITemplateFactory) : obj = 
//            Console.WriteLine("KWH");
            match ast with
            | Ast.Tree.Invoke (prop,parameters,debugInfo) ->
                match prop with
                | Ast.Tree.Property (expr,identifier,debugInfo) ->
                    let newObj = materializePropertyOfParent parent parentAst name expr factory
                    attachMetadataFromInvoke factory newObj ast expr identifier parameters
                    newObj
                | _ ->
                    failedExpectation1 "Property" prop
            | _ ->

               let newObj = createTemplate parent name ast factory
//               Console.WriteLine( "Created property " + parentStr + "." + name + "=" + newObj.GetType().Name + " (ast=" + ast.ToString() + ")" );

               match ast with
               | Ast.Object (properties,p) ->
                   properties |> List.iter 
                       begin
                           fun ( name, expression ) ->
//                              Console.WriteLine( "Adding property " + name );
                              materializePropertyOfParent newObj ast name expression factory |> ignore
                       end
                   newObj
               | Ast.Array (elements,debugInfo) ->
                   elements |> List.iter 
                       begin
                           fun ( expression ) ->
//                               Console.WriteLine( "Adding element " + name );
                               materializePropertyOfParent newObj ast null expression factory |> ignore
                       end
                   newObj
               | _ ->
                   newObj
        
        let rec interpretBlockItem (assignBlockOrObject:Ast.Tree) (factory:ITemplateFactory) (restrictToDesigntimeVariable:bool) = // Takes the Ast tree containing the template script. I.e. __template__ = { FirstName:'Joachim', LastName:'Wester' }
            match assignBlockOrObject with
            | Ast.Tree.Block stmtList ->
                assert ( stmtList.Length = 1 )
                let var = ( stmtList.Item(0) )
                match var with
                | Ast.Tree.Var assign ->
                    match assign with
                        | Ast.Tree.Assign (identifier, expression) ->                       // $$DESIGNTIME$$ = { ... }
                            match identifier with
                            | Ast.Identifier (str,debugInfo) ->
                                if (not restrictToDesigntimeVariable || str.Equals("$$DESIGNTIME$$") )  then
                                    // For "js-only" sources, any assignment will be assumed to be view model. If embedded, the name of the variable must be $$DESIGNTIME$$
                                    interpretBlockItem expression factory false
                                else
                                    null
                            | _ -> failedExpectation restrictToDesigntimeVariable "$$DESIGNTIME$$" identifier
                        | _ -> failedExpectation restrictToDesigntimeVariable "assign" assign
                | _ -> failedExpectation restrictToDesigntimeVariable "var" var
            | _ ->
                if (not restrictToDesigntimeVariable) then
                   let parent = materializePropertyOfParent null (Ast.Null( DebugInfo(0,0,""))) null assignBlockOrObject factory                                // Heureka! Here is the Javascript like object expression creating the template
                   parent
                else
                   null



        let interpretRootFunction (astTree:Ast.Tree) (factory:ITemplateFactory) (notEmbedded:bool)  = // Takes the Ast tree containing the template scrit. I.e. __template__ = { FirstName:'Joachim', LastName:'Wester' }
            match astTree with
            | Ast.Tree.Function (a,b,block,debugInfo) ->
                    match block with
                    | Ast.Tree.Block (blockListWithEmptyBlocks) ->
                        let blockList = // Lets filter out all empty blocks (pass)
                            List.filter  (fun block -> match block with | Ast.Pass -> false | _ -> true ) blockListWithEmptyBlocks
                        if (blockList.Length = 0) then
                            failwith "The js template is empty. Please provide a template object declaration." 
                        assert ( blockList.Length = 1 )
                        let assignBlockOrObject = ( blockList.Item(0) )
                        interpretBlockItem assignBlockOrObject factory notEmbedded
                    | _ -> 
                        failedExpectation notEmbedded "Block" block
            | _ -> 
                failedExpectation notEmbedded "function" astTree
        let result = interpretRootFunction astTree factory ignoreNonDesignTimeAssignments
        result
  
    

    
