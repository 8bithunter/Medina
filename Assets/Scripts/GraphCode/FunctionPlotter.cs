using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ComplexUnity = System.Numerics.Complex;
using System.Numerics;
using System;

public class FunctionPlotter : MonoBehaviour
{
    public GameObject pointPrefab;
    public TMP_Text inputText;
    public TMP_Text outputText;
    public TMP_Text text;
    public Transform originDot;
    public int resolution = 100;

    private List<GameObject> points = new List<GameObject>();

    private UnityEngine.Vector3 dragStart;
    private bool dragging = false;

    public bool invertFunction = false;

    // Use the same offsets as your grid (make these public or link from Scaler if needed)
    private ComplexUnity inputOffset = ComplexUnity.Zero;
    private float outputOffset = 0;

    private double lastScale = -1;

    void Update()
    {
        bool shouldReplot = false;

        // Handle drag start
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0;
            dragging = true;
        }

        // Handle drag move - update offsets dynamically
        if (dragging && Input.GetMouseButton(0))
        {
            UnityEngine.Vector3 current = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            current.z = 0;
            UnityEngine.Vector3 delta = current - dragStart;
            dragStart = current;

            // Match your grid behavior: inputOffset += delta.x, outputOffset += delta.y (Desmos style)
            inputOffset -= new ComplexUnity(delta.x, 0);
            outputOffset += delta.y;

            shouldReplot = true;
        }

        // Handle drag end
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        // Handle zoom change (compare with last scale)
        if (Scaler.scale != lastScale)
        {
            lastScale = Scaler.scale;
            shouldReplot = true;
        }

        // Replot if needed
        if (shouldReplot)
        {
            ClearPoints();
            PlotFunction();

            // Update coordinates text and origin dot position live
            originDot.position = new UnityEngine.Vector3(-(float)inputOffset.Real, outputOffset, 0);
        }

        // Show nearest point only while holding right mouse button (optional)
        if (Input.GetMouseButton(1))
        {
            ShowNearestPoint();
        }
    }

    void PlotFunction()
    {
        // Optional: limit x-range to visible area to optimize performance
        Camera cam = Camera.main;
        UnityEngine.Vector3 bottomLeft = cam.ScreenToWorldPoint(new UnityEngine.Vector3(0, 0, 0));
        UnityEngine.Vector3 topRight = cam.ScreenToWorldPoint(new UnityEngine.Vector3(Screen.width, Screen.height, 0));
        float xMin = bottomLeft.x;
        float xMax = topRight.x;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float x = Mathf.Lerp(xMin, xMax, t);

            ComplexUnity z = new ComplexUnity(x, 0);

            ComplexUnity result;
            ComplexUnity deriv = 0;

            // Parse the function once outside loop for efficiency if possible
            // For demonstration, keeping inside loop (replace with caching in production)
            var parsedFunction = FunctionParser.Parse(GraphStackInterpreter.inputFunction);

            if (!invertFunction)
            {
                // Apply input offset and scale
                double shiftedInput = (z + inputOffset).Real * Scaler.scale;
                result = FunctionsOfFunctions.Evaluate(parsedFunction, shiftedInput) / Scaler.scale;

                // Apply output offset
                result += new ComplexUnity(outputOffset, 0);

                deriv = Derivative(parsedFunction, shiftedInput);
            }
            else
            {
                double shiftedInput = (z - outputOffset).Real * Scaler.scale;
                result = FunctionsOfFunctions.Evaluate(parsedFunction, shiftedInput) / Scaler.scale;
                result += new ComplexUnity(-inputOffset.Real, 0);

                deriv = Derivative(parsedFunction, shiftedInput);
            }

            float y = (float)result.Real;

            // Clamp huge values for stability
            y = Mathf.Clamp(y, -1000000f, 1000000f);
            x = Mathf.Clamp(x, -1000000f, 1000000f);

            UnityEngine.Vector3 pos = new UnityEngine.Vector3(x, y, 0);

            float angle = Mathf.Atan2((float)(deriv.Real) * (float)Scaler.scale, 1) * Mathf.Rad2Deg;

            if (invertFunction)
            {
                pos = new UnityEngine.Vector3(y, x, 0);
                angle = 90 - angle;
            }

            GameObject point = Instantiate(pointPrefab, pos, UnityEngine.Quaternion.Euler(0, 0, angle));
            point.transform.SetParent(this.transform);
            points.Add(point);
        }
    }

    public static Complex Derivative(FunctionTree functionTree, double x)
    {
        double dx = 1e-8;
        Complex fz_plus_h = FunctionsOfFunctions.Evaluate(functionTree, x + dx);
        Complex fz_minus_h = FunctionsOfFunctions.Evaluate(functionTree, x - dx);
        return (fz_plus_h - fz_minus_h) / (2 * dx) / Scaler.scale;
    }

    void ClearPoints()
    {
        foreach (var point in points)
            Destroy(point);
        points.Clear();
    }

    void ShowNearestPoint()
    {
        if (points.Count == 0) return;

        UnityEngine.Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        GameObject closest = points[0];
        float minDist = UnityEngine.Vector3.Distance(mouseWorldPos, closest.transform.position);

        foreach (GameObject point in points)
        {
            float dist = UnityEngine.Vector3.Distance(mouseWorldPos, point.transform.position);
            if (dist < minDist)
            {
                closest = point;
                minDist = dist;
            }
        }

        UnityEngine.Vector3 coord = closest.transform.position;

        inputText.text = $"{(coord.x + inputOffset.Real) * Scaler.scale:0.000}";
        outputText.text = $"{(coord.y - outputOffset) * Scaler.scale:0.000}";
    }
}
