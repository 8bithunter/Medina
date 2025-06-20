using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FunctionTree : MonoBehaviour
{
    public Function function; // basically just a string "add"
    public List<FunctionTree> children;

    public FunctionTree(Function funtion)
    {
        this.function = funtion;
    }

    public void AddChild(FunctionTree tree)
    {
        this.children.Add(tree);
    }
}
