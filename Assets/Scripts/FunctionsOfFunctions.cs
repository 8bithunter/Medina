using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class FunctionsOfFunctions : MonoBehaviour
{
    FunctionTree Differentiate(FunctionTree tree)
    {
        FunctionTree placeholderTree = new FunctionTree(tree.function);
        FunctionTree differentiatedTree = new FunctionTree(DifferentiateFunction(tree.function));
        for (int i = 0; i < tree.children.Count; i++)
        {
            placeholderTree.AddChild(Differentiate(tree.children[i]));
            differentiatedTree.AddChild(tree.children[i]);
        }
        return null; //placeholderTree * differentiatedTree
    }

    Function DifferentiateFunction(Function function)
    {
        return null; //big ass table
    }

    /*
     * Function.name = Cos
     * 
     * if (fucntion.name == "cos") return "-sin"
     * 
     * 
    */
}
