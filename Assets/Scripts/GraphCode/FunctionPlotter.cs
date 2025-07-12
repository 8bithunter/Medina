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

    private float timer = 0f;
    public float interval = 0.2f;

    private double lastScale = -1;
    private List<GameObject> points = new List<GameObject>();

    private UnityEngine.Vector3 dragStart;
    private UnityEngine.Vector3 dragEnd;
    private bool dragging = false;

    public bool invertFunction = false;

    private ComplexUnity inputOffset = ComplexUnity.Zero;
    private float outputOffset = 0;

    void Update()
    {
        // Replot if scale changes
        if (Scaler.scale != lastScale)
        {
            lastScale = Scaler.scale;
            ClearPoints();
            PlotFunction();
        }

        // Begin dragging
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0;
            dragging = true;
        }

        // End drag and apply offset
        if (Input.GetMouseButtonUp(0) && dragging)
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z = 0;
            dragging = false;

            UnityEngine.Vector3 delta = dragEnd - dragStart;

            // x-movement → shift input: f(z - dx)
            inputOffset -= new ComplexUnity(delta.x, 0);

            // y-movement → shift output: f(z) + dy
            outputOffset += delta.y;

            ClearPoints();
            PlotFunction();
        }

        // Show closest point only when not draggawing
        if (Input.GetMouseButton(1))
        {
            ShowNearestPoint();
        }

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer -= interval;
            ClearPoints();
            PlotFunction();

            text.text = "(" + Math.Round(inputOffset.Real * Scaler.scale, 3) + ", " + Math.Round(-(double)outputOffset * Scaler.scale, 3) + ")";
            originDot.position = new UnityEngine.Vector3(-(float)inputOffset.Real, outputOffset, 0);
        }
    }

    void PlotFunction()
    {
        float xMin = -9f;
        float xMax = 9f;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float x = Mathf.Lerp(xMin, xMax, t);

            ComplexUnity z = new ComplexUnity(x, 0);

            ComplexUnity result;
            ComplexUnity deriv = 0;

            GraphStackInterpreter blockParser = new GraphStackInterpreter();

            if (!invertFunction)
            {
                // Apply input shift
                result = FunctionsOfFunctions.Evaluate(FunctionParser.Parse(GraphStackInterpreter.inputFunction), (z + inputOffset).Real * Scaler.scale) / Scaler.scale;

                // Apply output shift
                result += new ComplexUnity(outputOffset, 0);

                deriv = Derivative(FunctionParser.Parse(GraphStackInterpreter.inputFunction), (z + inputOffset).Real * Scaler.scale);
            }
            else
            {
                // Apply input shift
                result = FunctionsOfFunctions.Evaluate(FunctionParser.Parse(GraphStackInterpreter.inputFunction), (z - outputOffset).Real * Scaler.scale) / Scaler.scale;

                // Apply output shift
                result += new ComplexUnity(-inputOffset.Real, 0);

                deriv = Derivative(FunctionParser.Parse(GraphStackInterpreter.inputFunction), (z - outputOffset).Real * Scaler.scale);
            }

                float y = (float)result.Real;

                if (Mathf.Abs(y) > 1000000) y = 1000000;
                if (Mathf.Abs(x) > 1000000) x = 1000000;

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

    public static Complex Derivative(FunctionTree functionTree, Complex z)
    {
        double dx = 0.00000001;
        Complex fz_plus_h = FunctionsOfFunctions.Evaluate(functionTree, (z + new Complex(dx, 0)).Real);
        Complex fz_minus_h = FunctionsOfFunctions.Evaluate(functionTree, (z - new Complex(dx, 0)).Real);

        return ((fz_plus_h - fz_minus_h) / (2 * dx)) / Scaler.scale;
    }

    void ClearPoints()
    {
        foreach (GameObject point in points)
        {
            Destroy(point);
        }
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

        if (!invertFunction)
        {
            inputText.text = $"{(coord.x + inputOffset.Real) * Scaler.scale:0.000}";
            outputText.text = $"{(coord.y - outputOffset) * Scaler.scale: 0.000}";
        }
        else
        {
            inputText.text = $"{(coord.x + inputOffset.Real) * Scaler.scale:0.000}";
            outputText.text = $"{(coord.y - outputOffset) * Scaler.scale: 0.000}";
        }
        
    }
}
