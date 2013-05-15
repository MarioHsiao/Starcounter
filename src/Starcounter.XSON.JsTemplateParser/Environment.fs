// Starcounter Internal f Ecmascript Parser
// Copyright (c) Starcounter AB


namespace Starcounter.Internal.JsonTemplate

open System

type public Environment = // JOCKE

    [<DefaultValue>]val mutable currentSchemaId : int
    [<DefaultValue>]val mutable currentFunctionId : int

    member public this.NextFunctionId ()  =
        this.currentFunctionId <- this.currentFunctionId + 1
        this.currentFunctionId

    member public this.NextPropertyMapId () =
        this.currentSchemaId <- this.currentSchemaId + 1
        this.currentSchemaId

    new() = {}

type Env = Environment
type Src = string * string * int // JOCKE javaScriptText, sourceFileReference, hiddenLeadingCode

///
module CustomOperators =

  let inline ($) a b = b a
  let inline (==) a b = Object.ReferenceEquals(a, b)
  let inline (!==) a b = not(Object.ReferenceEquals(a, b))
  
///
module Aliases = 
  
  open System.Globalization
  open System.Collections.Generic

  type MutableList<'a> = List<'a>
  type MutableStack<'a> = Stack<'a>
  type MutableDict<'k, 'v> = Dictionary<'k, 'v>
  type MutableSorted<'k, 'v> = SortedDictionary<'k, 'v>

  #if LEGACY_HASHSET
  type MutableSet<'a when 'a : equality> = HashSet<'a>
  #else
  type MutableSet<'a> = HashSet<'a>
  #endif
  
  let anyNumber = NumberStyles.Any
  let invariantCulture = CultureInfo.InvariantCulture
  let currentCulture = CultureInfo.CurrentCulture

  let NaN = Double.NaN
  let NegInf = Double.NegativeInfinity 
  let PosInf = Double.PositiveInfinity
 
