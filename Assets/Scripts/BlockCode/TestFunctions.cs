using UnityEngine;
using System;
public class TestFunctions : MonoBehaviour
{
    [TextArea(1, 3)]
    public string inputExpression = "sin(x^2 / e^x)";

    [Tooltip("The value of x at which to evaluate the original and derivative functions.")]
    public double xValue = 1.0;

    void Start()
    {
        try
        {
            // 1. Parse
            FunctionTree tree = FunctionParser.Parse(inputExpression);
            tree = FunctionTreeStringifier.StringCompressor(tree);

            // 2. Differentiate
            FunctionTree derivative = FunctionsOfFunctions.Differentiate(tree);

            // 3. Stringify
            string originalStr = FunctionTreeStringifier.ToReadableString(tree);
            string derivativeStr = FunctionTreeStringifier.ToReadableString(derivative);

            // 4. Evaluate
            double originalVal = FunctionsOfFunctions.Evaluate(tree, xValue);
            double derivativeVal = FunctionsOfFunctions.Evaluate(derivative, xValue);

            // 5. Output
            Debug.Log("Input Expression:     " + originalStr);
            Debug.Log("Derivative Expression:" + derivativeStr);
            Debug.Log($"Value at x = {xValue}:");
            Debug.Log("   Original: " + originalVal);
            Debug.Log("   Derivative: " + derivativeVal);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error: " + ex.Message);
        }
    }
}

