using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GraphStackInterpreter : MonoBehaviour
{
    public static string inputFunction = "x ^ 2";
    void Update()
    {
        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Block");

        foreach (GameObject block in allBlocks)
        {
            TMP_InputField inputField = block.GetComponentInChildren<TMP_InputField>();
            if (inputField == null) continue;

            string text = inputField.text.Trim().ToLower();
            if (text == "graph")
            {
                string parsed = ParseBlockStackExpression(block.transform);
                Debug.Log($"Parsed Expression: {parsed}");
                GraphStackInterpreter.inputFunction = parsed;
            }
        }
    }

    /// <summary>
    /// Parses an expression by walking down from the graph block through its children.
    /// </summary>
    public static string ParseBlockStackExpression(Transform topBlock)
    {
        if (topBlock == null) return "";

        // Step 1: Build stack from top to bottom
        List<Transform> stack = new List<Transform>();
        Transform current = topBlock;

        while (current != null)
        {
            stack.Add(current);
            current = GetChildBlock(current);
        }

        // Step 2: Remove the "graph" block from top
        if (stack.Count > 0 && GetInput(stack[0]).Trim().ToLower() == "graph")
            stack.RemoveAt(0);

        if (stack.Count == 0)
            return "x"; // default if only "graph" present

        // Step 3: Start from the BOTTOM and wrap upward
        string result = GetInput(stack[stack.Count - 1]);

        for (int i = stack.Count - 2; i >= 0; i--)
        {
            string input = GetInput(stack[i]);

            if (input.Contains("()"))
            {
                input = input.Replace("()", $"({result})");
            }
            else
            {
                input += result;
            }

            result = input;
        }

        return result;
    }

    private static string GetInput(Transform block)
    {
        TMP_InputField field = block.GetComponentInChildren<TMP_InputField>();
        return field != null ? field.text.Trim() : "";
    }

    private static Transform GetChildBlock(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag("Block"))
                return child;
        }
        return null;
    }
}