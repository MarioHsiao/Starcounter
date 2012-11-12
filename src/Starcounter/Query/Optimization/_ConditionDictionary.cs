// ***********************************************************************
// <copyright file="_ConditionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
/// <summary>
/// Class that holds a dictionary of condition lists where the key of a condition list
/// corresponds to the extents referenced to in the conditions in that list.
/// </summary>
class ConditionDictionary
{
    Dictionary<UInt64, List<ILogicalExpression>> dictionary;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal ConditionDictionary()
    {
        dictionary = new Dictionary<UInt64, List<ILogicalExpression>>();
    }

    /// <summary>
    /// Divide a condition into subconditions separated by the AND operator,
    /// and adds each subcondition at the appropriate place in the dictionary.
    /// </summary>
    /// <param name="condition">The condition to be added.</param>
    internal void AddCondition(ILogicalExpression condition)
    {
        if (condition is LogicalOperation && (condition as LogicalOperation).Operator == LogicalOperator.AND)
        {
            AddCondition((condition as LogicalOperation).Expression1);
            AddCondition((condition as LogicalOperation).Expression2);
        }
        else if (!(condition is LogicalLiteral) || (condition.Evaluate(null) != TruthValue.TRUE))
        {
            // Get the extents that are referenced in the current condition.
            ExtentSet extentSet = new ExtentSet();
            condition.InstantiateExtentSet(extentSet);
            // Add the current condition at an appropriate place in the dictionary.
            List<ILogicalExpression> currentConditionList = null;
            if (dictionary.TryGetValue(extentSet.Value, out currentConditionList))
            {
                currentConditionList.Add(condition);
            }
            else
            {
                currentConditionList = new List<ILogicalExpression>();
                currentConditionList.Add(condition);
                dictionary.Add(extentSet.Value, currentConditionList);
            }
        }
    }

    /// <summary>
    /// Gets a list of conditions with the input extent set as key, and returns null if there is no such list.
    /// </summary>
    /// <param name="key">The key for which a list of conditions is looked for.</param>
    /// <returns>A found list of conditions or null.</returns>
    internal List<ILogicalExpression> GetConditions(ExtentSet key)
    {
        if (dictionary.ContainsKey(key.Value))
        {
            List<ILogicalExpression> conditionList = dictionary[key.Value];
            return conditionList;
        }
        return null;
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    internal bool AssertEquals(ConditionDictionary other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check cardinalities of collections
        Debug.Assert(this.dictionary.Count == other.dictionary.Count);
        if (this.dictionary.Count != other.dictionary.Count)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        // Check collections of objects
        foreach (KeyValuePair<UInt64, List<ILogicalExpression>> kvp in this.dictionary) {
            List<ILogicalExpression> otherVal;
            Debug.Assert(other.dictionary.TryGetValue(kvp.Key, out otherVal));
            areEquals = otherVal != null;
            if (areEquals) {
                Debug.Assert(kvp.Value.Count == otherVal.Count);
                areEquals = kvp.Value.Count == otherVal.Count;
            }
            for (int i = 0; i < kvp.Value.Count && areEquals; i++)
                areEquals = kvp.Value[i].AssertEquals(otherVal[i]);
        }
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
