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
    public Material solidLineMaterial;
    public Material dottedLineMaterial;

    private List<GameObject> points = new List<GameObject>();
    private LineRenderer lineRenderer;

    private UnityEngine.Vector3 dragStart;
    private bool dragging = false;

    public bool invertFunction = false;

    private ComplexUnity inputOffset = ComplexUnity.Zero;
    private float outputOffset = 0;

    private double lastScale = -1;

    void Update()
    {
        bool shouldReplot = false;

        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0;
            dragging = true;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            UnityEngine.Vector3 current = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            current.z = 0;
            UnityEngine.Vector3 delta = current - dragStart;
            dragStart = current;

            inputOffset -= new ComplexUnity(delta.x, 0);
            outputOffset += delta.y;

            shouldReplot = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        if (Scaler.scale != lastScale)
        {
            lastScale = Scaler.scale;
            shouldReplot = true;
        }

        if (shouldReplot)
        {
            ClearPoints();
            PlotFunction();
            originDot.position = new UnityEngine.Vector3(-(float)inputOffset.Real, outputOffset, 0);
        }

        if (Input.GetMouseButton(1))
        {
            ShowNearestPoint();
        }
    }

    void PlotFunction()
    {
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

            var parsedFunction = FunctionParser.Parse(GraphStackInterpreter.inputFunction);

            if (!invertFunction)
            {
                double shiftedInput = (z + inputOffset).Real * Scaler.scale;
                result = FunctionsOfFunctions.Evaluate(parsedFunction, shiftedInput) / Scaler.scale;
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

        float verticalThreshold = 2f;
        for (int i = 1; i < points.Count; i++)
        {
            UnityEngine.Vector3 prev = points[i - 1].transform.position;
            UnityEngine.Vector3 curr = points[i].transform.position;
            bool isJump = Mathf.Abs(curr.y - prev.y) >= verticalThreshold;

            GameObject lineObj = new GameObject("FunctionSegment");
            lineObj.transform.SetParent(this.transform);
            LineRenderer segLine = lineObj.AddComponent<LineRenderer>();

            segLine.material = isJump ? dottedLineMaterial : solidLineMaterial;
            segLine.widthMultiplier = 0.05f;
            segLine.startColor = Color.white;
            segLine.endColor = Color.white;
            segLine.useWorldSpace = true;
            segLine.positionCount = 2;
            segLine.SetPositions(new UnityEngine.Vector3[] { prev, curr });
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

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("FunctionSegment"))
                Destroy(child.gameObject);
        }
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
