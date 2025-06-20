using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class FunctionsOfFunctions : MonoBehaviour
{
    FunctionTree Differentiate(FunctionTree tree)
    {
        for (int i = 0; i < tree.children.Count; i++)
        {
                Differentiate(tree.children[i]);
        }
        return DifferentiateFunction(tree.function);
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
