using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using ComplexUnity = System.Numerics.Complex;

public class Scaler : MonoBehaviour
{
    public static double scale = 1;
    private double minScale = 0.1;
    private double maxScale = 10;

    public GameObject input;
    private float orginalscale1;
    private float orginalscale2;

    private Vector3 dragStart;
    private bool dragging = false;

    private ComplexUnity inputOffset = ComplexUnity.Zero; // For horizontal movement
    private float outputOffset = 0; // For vertical movement


    private float lineThickness = 0.02f;
    public GameObject squarePrefab;

    private List<GameObject> gridLines = new List<GameObject>();

    private List<GameObject> xLabels = new List<GameObject>();
    private List<GameObject> yLabels = new List<GameObject>();



    private void Start()
    {
        PlotGraph();
    }

    void Update()
    {
        bool shouldRedraw = false;

        // Handle mouse drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0;
            dragging = true;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            Vector3 current = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            current.z = 0;

            Vector3 delta = current - dragStart;
            dragStart = current;

            // Invert delta for Desmos-style "grab and drag"
            inputOffset += new ComplexUnity(delta.x, 0);
            outputOffset += delta.y;

            shouldRedraw = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        // Zoom with scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Math.Abs(scroll) > 0.001f)
        {
            double newScale = scale * (1 + -scroll * 0.3);
            BoundedScale(newScale);
            shouldRedraw = true;
        }

        // Zoom with Ctrl + = or Ctrl + -
        if ((Input.GetKeyDown(KeyCode.Equals) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
        {
            BoundedScale(scale * 0.5);
            shouldRedraw = true;
        }
        else if ((Input.GetKeyDown(KeyCode.Minus) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
        {
            BoundedScale(scale * 2);
            shouldRedraw = true;
        }

        // Update grid if necessary
        if (shouldRedraw)
        {
            ClearGrid();
            PlotGraph();
        }

    }

    public void BoundedScale(double newScale)
    {
        scale = Math.Clamp(newScale, minScale, maxScale);
    }

    public void PlotGraph()
    {
        double roundedScale = RoundToNearest125(scale) / scale;

        double gridSpacing = RoundToNearest125(scale);
        float xOffset = (float)inputOffset.Real;
        float yOffset = outputOffset;

        // Determine visible mathematical window (not Unity units)
        Camera cam = Camera.main;
        Vector3 bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0));
        Vector3 topRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height));

        double mathMinX = (bottomLeft.x - xOffset) * scale;
        double mathMaxX = (topRight.x - xOffset) * scale;

        double mathMinY = (bottomLeft.y - yOffset) * scale;
        double mathMaxY = (topRight.y - yOffset) * scale;

        // Start from nearest grid multiple
        double startX = Math.Floor(mathMinX / gridSpacing) * gridSpacing;
        double endX   = Math.Ceiling(mathMaxX / gridSpacing) * gridSpacing;

        double startY = Math.Floor(mathMinY / gridSpacing) * gridSpacing;
        double endY   = Math.Ceiling(mathMaxY / gridSpacing) * gridSpacing;


        // Vertical lines and X-axis labels
        for (double gx = startX; gx <= endX; gx += gridSpacing)
        {
            float worldX = (float)(gx / scale + xOffset); // convert from math to Unity
            bool isAxis = Math.Abs(gx) < 1e-6;

            CreateLine(worldX, 0, isVertical: true, isAxis);
            CreateLabel(worldX - 0.15f, yOffset, gx, true, isAxis);
        }

        // Horizontal lines and Y-axis labels
        for (double gy = startY; gy <= endY; gy += gridSpacing)
        {
            float worldY = (float)(gy / scale + yOffset);
            bool isAxis = Math.Abs(gy) < 1e-6;

            CreateLine(0, worldY, isVertical: false, isAxis);
            CreateLabel(xOffset, worldY - 0.15f, gy, false, isAxis);
        }
    }

    private void CreateLine(float x, float y, bool isVertical, bool isAxis)
    {
        GameObject square = Instantiate(squarePrefab, new Vector3(x, y, 0), Quaternion.identity);
        square.transform.localScale = isVertical
            ? new Vector3(lineThickness, 20, 1)
            : new Vector3(20, lineThickness, 1);

        if (isAxis)
            square.GetComponent<SpriteRenderer>().color = Color.white;

        gridLines.Add(square);
    }

    public void ClearGrid()
    {
        foreach (GameObject line in gridLines)
            Destroy(line);
        gridLines.Clear();

        foreach (GameObject label in xLabels)
            Destroy(label);
        xLabels.Clear();

        foreach (GameObject label in yLabels)
            Destroy(label);
        yLabels.Clear();
    }

    private double RoundToNearest125(double value)
    {
        if (value == 0) return 0;

        double absVal = Math.Abs(value);
        double log10 = Math.Log10(absVal);
        int minExp = (int)Math.Floor(log10) - 1;
        int maxExp = (int)Math.Ceiling(log10) + 2;

        double[] steps = { 1, 2, 5 };
        double closest = 0;
        double minDiff = double.MaxValue;

        for (int exp = minExp; exp <= maxExp; exp++)
        {
            double base10 = Math.Pow(10, exp);
            foreach (double step in steps)
            {
                double candidate = step * base10;
                double diff = Math.Abs(candidate - absVal);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = candidate;
                }
            }
        }

        return Math.Sign(value) * closest;
    }

    private void CreateLabel(float x, float y, double value, bool isXAxis, bool isAxisLine)
    {
        GameObject labelObj = new GameObject("GridLabel");
        labelObj.transform.SetParent(this.transform, false);
        labelObj.transform.position = new Vector3(x, y, 0);

        var tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = FormatLabel(value);
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.75f, 0.75f, 0.75f);
        tmp.enableWordWrapping = false;

        // Shift away from axis
        Vector3 offset = isXAxis ? new Vector3(0, -0.15f, 0) : new Vector3(-0.15f, 0, 0);
        labelObj.transform.position += offset;

        if (isAxisLine)
            tmp.fontStyle = FontStyles.Bold;

        // Fixed scale (small)
        labelObj.transform.localScale = Vector3.one * 0.1f;

        if (isXAxis)
            xLabels.Add(labelObj);
        else
            yLabels.Add(labelObj);
    }

    private string FormatLabel(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value - Math.Round(value)) < 1e-6)
            return Math.Round(value).ToString("0");
        return value.ToString("0.###");
    }

}
