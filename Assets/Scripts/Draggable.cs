using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Draggable : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private bool isDragging = false;

    void Start()
    {
        cam = Camera.main;
    }

    void OnMouseDown()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z);
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z) + offset;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
    }
}