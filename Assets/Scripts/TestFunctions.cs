using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFunctions : MonoBehaviour
{
    [TextArea(1, 3)]
    public string inputExpression = "sin(x^2 / e^x)";

    void Start()
    {
        try
        {
            // 1. Parse
            var parser = new FunctionParser();
            FunctionTree tree = parser.Parse(inputExpression);

            // 2. Differentiate
            FunctionTree derivative = FunctionsOfFunctions.Differentiate(tree);

            // 3. Stringify
            string originalStr = FunctionTreeStringifier.ToReadableString(tree);
            string derivativeStr = FunctionTreeStringifier.ToReadableString(derivative);

            // 4. Output
            Debug.Log("Input:      " + originalStr);
            Debug.Log("Derivative: " + derivativeStr);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error: " + ex.Message);
        }
    }
}
