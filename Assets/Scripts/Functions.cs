using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FunctionTree
{
    public Function function;
    public List<FunctionTree> children;

    public FunctionTree(Function function)
    {
        this.function = function;
        this.children = new List<FunctionTree>(); 
    }

    public void AddChild(FunctionTree tree)
    {
        this.children.Add(tree);
    }
}
