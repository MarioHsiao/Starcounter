// ***********************************************************************
// <copyright file="ParseTreeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using Starcounter.CodeGeneration.Serialization;


namespace Starcounter.Internal.Application.CodeGeneration.Serialization {

    /// <summary>
    /// For fast execution, Starcounter will generate code to match incomming HTTP requests.
    /// By matching and parsing the verb and URI, the correct user handler delegate will be called.
    /// This class is responsible to accept registration of user handlers and also to
    /// generate code for and instantiate the top level and sub level RequestProcessors
    /// needed to perform this task. The code generation and instantiation is performed as late
    /// as possible. If additional handlers are registred after code has been generated, a new
    /// version is generated and replaces the old version.
    /// </summary>
    public static class ParseTreeGenerator {

        /// <summary>
        /// Builds a branch tree from a flat list of user registred rest style handlers
        /// </summary>
        /// <param name="handlers">The meta data built up from the registred handlers before code generation is performed</param>
        /// <returns>The branch tree used to generate the code for the request processor used to match the verbs and URIs</returns>
        public static ParseNode BuildParseTree(List<TemplateMetadata> handlers) {
            var n = new ParseNode() {
                AllHandlers = handlers
            };
            CreateChildren(n, handlers, handlers, 0);
            return n;
        }

        /// <summary>
        /// Helper function for CreateChildren to optimize the sorting an grouping
        /// where all handlers have the same characters.
        /// </summary>
        /// <param name="handlers">The verbs and uris to compare</param>
        /// <param name="characterIndex">The index</param>
        /// <returns>True if all verbs and uris have the same byte</returns>
        /// <remarks>Detects that all handlers in the supplied list has the same byte at a given
        /// position. Used to find forks (branching) between verbs and uris.
        /// The function also reports a difference if the special parse character '@'
        /// is encountered or if the character is a parse type character (i.e. its
        /// previous character is the special parse character '@')</remarks>
        public static CompareVU DetectIgnoreBranch(IEnumerable<RequestProcessorMetaData> handlers, int characterIndex) {
            byte before = 0;
            bool eos = true;
            foreach (var h in handlers) {
                byte[] vuri = Encoding.UTF8.GetBytes(h.PreparedVerbAndUri);
                if (characterIndex < vuri.Length) {
                    eos = false;
                    var c = vuri[characterIndex];
                    if (c == '@' || (before != 0 && (c != before))) { // Earlier we replaced the {x} parameters with the @ character
                        return CompareVU.CreateLowerLevel;
                    }
                    else if (characterIndex > 0 && vuri[characterIndex - 1] == '@')
                        return CompareVU.CreateLowerLevel;
                    before = vuri[characterIndex];
                }
            }
            return (eos) ? CompareVU.IgnoreEOS : CompareVU.Ignore;
        }

        /// <summary>
        /// Internal help function to group handlers into forks on a certain byte in the verb and uri UTF8 string.
        /// </summary>
        /// <param name="handlers">The handlers to group</param>
        /// <param name="i">The byte to match</param>
        /// <returns>The groups</returns>
        public static Dictionary<byte, List<RequestProcessorMetaData>> Group(IEnumerable<RequestProcessorMetaData> handlers, int i) {
            var ret = new Dictionary<byte, List<RequestProcessorMetaData>>();
            foreach (var h in handlers) {
                byte[] vuri = Encoding.UTF8.GetBytes(h.PreparedVerbAndUri);
                if (vuri.Length > i) {
                    byte c = vuri[i];
                    if (!ret.ContainsKey(c))
                        ret[c] = new List<RequestProcessorMetaData>();
                    ret[c].Add(h);
                }
//                else {
//                    if (!ret.ContainsKey(0))
//                        ret[0] = new List<RequestProcessorMetaData>();
//                    ret[0].Add(h);
//                }
            }
            return ret;
        }

        /// <summary>
        /// Called internally to build the tree
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="allHandlers">All handlers.</param>
        /// <param name="handlers">The verbs and uris to match</param>
        /// <param name="characterIndex">Index of the character.</param>
        public static void CreateChildren(ParseNode node, List<RequestProcessorMetaData> allHandlers, IEnumerable<RequestProcessorMetaData> handlers, int characterIndex) {
            var s = CompareVU.Ignore;
            var origin = characterIndex;
            while (true) {
                s = DetectIgnoreBranch(handlers, characterIndex);
                if (s == CompareVU.IgnoreEOS) {
                    foreach (var g in handlers) {
                        var n = new ParseNode() {
                            Match = 0,
                            HandlerIndex = allHandlers.IndexOf(g),
                            AllHandlers = allHandlers
                        };
                        node.Candidates.Add(n);
                        n.Parent = node;
                        n.MatchCharInTemplateAbsolute = characterIndex - 1;
                    }
                    return;
                }
                if (s == CompareVU.CreateLowerLevel) {
                    var groups = Group(handlers, characterIndex);
                    foreach (var g in groups) {
                        var n = new ParseNode() {
                            Match = g.Key,
//                            DebugMatchString = g.Value[0].verbAndUri.Substring(origin, characterIndex + 1 - origin),
                            AllHandlers = allHandlers
                        };
                        node.Candidates.Add(n);
                        n.Parent = node;
                        if (g.Value.Count == 1 && n.DetectedType == NodeType.Heureka) {//n.DetectedType != NodeType.ParseNode && n.DetectedType != NodeType.TestAllCandidatesNode)
                            n.HandlerIndex = g.Value[0].HandlerIndex;
                        }

                        n.MatchCharInTemplateAbsolute = characterIndex;

//                        else {
//                            CreateChildren(n, allHandlers, g.Value, characterIndex + 1);
//                        }
                        CreateChildren(n, allHandlers, g.Value, characterIndex + 1);
                    }
                    break;
                }
                characterIndex++;
            }
        }
    }

    /// <summary>
    /// Internal status code when comparing verbs and uris
    /// </summary>
    public enum CompareVU {

        /// <summary>
        /// All handlers have the same character and there is no parsing
        /// needed
        /// </summary>
        Ignore,

        /// <summary>
        /// All handlers have the same character and there is no parsing
        /// needed and we have reach the end of the stream
        /// </summary>
        IgnoreEOS,

        /// <summary>
        /// At least one handler has a different character than the others
        /// or there is some parsing nodes needed
        /// </summary>
        CreateLowerLevel
    }
}