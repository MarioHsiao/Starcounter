﻿// ***********************************************************************
// <copyright file="ParseTreeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// For fast execution, Starcounter will generate code to handle serialization/deserialization
    /// of typed json instances.
    /// This class is responsible to accept registration of user json templates and also to
    /// generate code needed to perform this task. The code generation and instantiation is performed 
    /// as late as possible. If additional templates are registred after code has been generated, a new
    /// version is generated and replaces the old version.
    /// </summary>
    internal static class ParseTreeGenerator {
        /// <summary>
        /// Builds a branch tree from a flat list of templates.
        /// </summary>
        /// <param name="handlers">
        /// The meta data built up from the typed json.
        /// </param>
        /// <returns>
        /// The branch tree used to generate the code for serialization/deserialization
        /// </returns>
        internal static ParseNode BuildParseTree(List<TemplateMetadata> templates) {
            var n = new ParseNode() {
                AllTemplates = templates
            };
            CreateChildren(n, templates, templates, 0);
            return n;
        }

        /// <summary>
        /// Helper function for CreateChildren to optimize the sorting an grouping
        /// where all templates have the same characters.
        /// </summary>
        /// <param name="handlers">The templates to compare</param>
        /// <param name="characterIndex">The index</param>
        /// <returns>
        /// True if the character in the name of the current index is the same 
        /// for all templates.
        /// </returns>
        /// <remarks>
        /// Detects that all templates in the supplied list has the same byte at a given
        /// position. Used to find forks (branching) between templates.
        /// </remarks>
        internal static CompareVU DetectIgnoreBranch(IEnumerable<TemplateMetadata> templates, int characterIndex) {
            byte before = 0;
            bool eos = true;
            foreach (var t in templates) {
                byte[] vuri = t.TemplateNameArr;
                if (characterIndex < vuri.Length) {
                    eos = false;
                    var c = vuri[characterIndex];
                    if ((before != 0 && (c != before))) { 
                        return CompareVU.CreateLowerLevel;
                    }
                    before = vuri[characterIndex];
                }
            }
            return (eos) ? CompareVU.IgnoreEOS : CompareVU.Ignore;
        }

        /// <summary>
        /// Internal help function to group handlers into forks on a certain byte 
        /// in the template name.
        /// </summary>
        /// <param name="handlers">The templates to group</param>
        /// <param name="characterIndex">The byte to match</param>
        /// <returns>All groups of templates</returns>
        internal static Dictionary<byte, List<TemplateMetadata>> Group(IEnumerable<TemplateMetadata> templates, int characterIndex) {
            var ret = new Dictionary<byte, List<TemplateMetadata>>();
            foreach (var t in templates) {
                byte[] vuri = t.TemplateNameArr;
                if (vuri.Length > characterIndex) {
                    byte c = vuri[characterIndex];
                    if (!ret.ContainsKey(c))
                        ret[c] = new List<TemplateMetadata>();
                    ret[c].Add(t);
                }
            }
            return ret;
        }

        /// <summary>
        /// Called internally to build the tree
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="allTemplates">All templates.</param>
        /// <param name="templates">The templates to match</param>
        /// <param name="characterIndex">Index of the character in the name to compare.</param>
        internal static void CreateChildren(ParseNode node, List<TemplateMetadata> allTemplates, IEnumerable<TemplateMetadata> templates, int characterIndex) {
            var s = CompareVU.Ignore;
            var origin = characterIndex;
            while (true) {
                s = DetectIgnoreBranch(templates, characterIndex);
                if (s == CompareVU.IgnoreEOS) {
                    foreach (var g in templates) {
                        var n = new ParseNode() {
                            Match = 0,
                            TemplateIndex = allTemplates.IndexOf(g),
                            AllTemplates = allTemplates
                        };
                        node.Candidates.Add(n);
                        n.Parent = node;
                        n.MatchCharInTemplateAbsolute = characterIndex - 1;
                    }
                    return;
                }
                if (s == CompareVU.CreateLowerLevel) {
                    var groups = Group(templates, characterIndex);
                    foreach (var g in groups) {
                        var n = new ParseNode() {
                            Match = g.Key,
                            AllTemplates = allTemplates
                        };
                        node.Candidates.Add(n);
                        n.Parent = node;
                        if (g.Value.Count == 1 && n.DetectedType == NodeType.Heureka) {
                            n.TemplateIndex = g.Value[0].TemplateIndex;
                        }

                        n.MatchCharInTemplateAbsolute = characterIndex;
                        CreateChildren(n, allTemplates, g.Value, characterIndex + 1);
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
    internal enum CompareVU {

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