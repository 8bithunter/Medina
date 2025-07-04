using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ComplexUnity = System.Numerics.Complex;

public class FunctionPlotter : MonoBehaviour
{
    public GameObject pointPrefab;
    public TMP_Text inputText;
    public TMP_Text outputText;
    public int resolution = 100;

    private double lastScale = -1;
    private List<GameObject> points = new List<GameObject>();

    private Vector3 dragStart;
    private Vector3 dragEnd;
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

            Vector3 delta = dragEnd - dragStart;

            // x-movement → shift input: f(z - dx)
            inputOffset -= new ComplexUnity(delta.x, 0);

            // y-movement → shift output: f(z) + dy
            outputOffset += delta.y;

            ClearPoints();
            PlotFunction();
        }

        // Show closest point only when not dragging
        if (Input.GetMouseButton(1))
        {
            ShowNearestPoint();
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

            if (!invertFunction)
            {
                // Apply input shift
                result = Funcs.Function(z + inputOffset);

                // Apply output shift
                result += new ComplexUnity(outputOffset, 0);

                deriv = Funcs.Derivative(z + inputOffset); // use transformed input
            }
            else
            {
                // Apply input shift
                result = Funcs.Function(z - outputOffset);

                // Apply output shift
                result += new ComplexUnity(-inputOffset.Real, 0);

                deriv = Funcs.Derivative(z - outputOffset); // use transformed input
            }

            float y = (float)result.Real;

            Vector3 pos = new Vector3(x, y, 0);

            float angle = Mathf.Atan2((float)(deriv.Real * Scaler.scale), 1) * Mathf.Rad2Deg;

            if(invertFunction)
            {
                pos = new Vector3(y, x, 0);
                angle = 90 - angle;
            }

            GameObject point = Instantiate(pointPrefab, pos, Quaternion.Euler(0, 0, angle));
            point.transform.SetParent(this.transform);
            points.Add(point);
        }
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

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        GameObject closest = points[0];
        float minDist = Vector3.Distance(mouseWorldPos, closest.transform.position);

        foreach (GameObject point in points)
        {
            float dist = Vector3.Distance(mouseWorldPos, point.transform.position);
            if (dist < minDist)
            {
                closest = point;
                minDist = dist;
            }
        }

        Vector3 coord = closest.transform.position;
        inputText.text = $"{coord.x * Scaler.scale:0.000}";
        outputText.text = $"{coord.y * Scaler.scale: 0.000}";
    }
}
