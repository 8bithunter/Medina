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

    void Update()
    {
        // Replot if scale changes
        if (Scaler.scale != lastScale)
        {
            lastScale = Scaler.scale;
            ClearPoints();
            PlotFunction();
        }

        // Show closest point on mouse hold
        if (Input.GetMouseButton(0))
        {
            ShowNearestPoint();
        }
        else
        {
            inputText.text = "";
            outputText.text = "";
        }
    }

    void PlotFunction()
    {
        float xMin = (float)(-9);
        float xMax = (float)(9);

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float x = Mathf.Lerp(xMin, xMax, t);

            ComplexUnity z = new ComplexUnity(x, 0);           // Real input only
            ComplexUnity result = Funcs.Function(z);           // Evaluate function
            float y = (float)result.Real;                 // Use imaginary part as output

            Vector3 pos = new Vector3(x, y, 0);                // Plot (x, Im[f(x)])
            GameObject point = Instantiate(pointPrefab, pos, Quaternion.identity);
            point.transform.SetParent(this.transform);         // Optional: parent under plotter
            points.Add(point);                                 // Store to delete later
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
