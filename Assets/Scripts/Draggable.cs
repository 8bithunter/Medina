using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Draggable : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private bool isDragging = false;

    private Block block;
    private HashSet<Block> draggingBlocks;

    void Start()
    {
        cam = Camera.main;
        block = GetComponent<Block>();
    }

    void OnMouseDown()
    {
        // Disconnect connections if needed (optional)
        // block.DisconnectAllConnections();

        // Get all blocks connected below this one (including self)
        draggingBlocks = block.GetBlocksBelowRecursive();

        offset = transform.position - GetMouseWorldPos();
        isDragging = true;

        // Set all dragged blocks to kinematic Rigidbody2D if not already
        foreach (Block b in draggingBlocks)
        {
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
                rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3 targetPos = mouseWorldPos + offset;

        foreach (Block b in draggingBlocks)
        {
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector3 relativePos = b.transform.position - block.transform.position;
                Vector3 newPos = targetPos + relativePos;
                rb.MovePosition(new Vector2(newPos.x, newPos.y));
            }
            else
            {
                // fallback, just in case
                b.transform.position = targetPos + (b.transform.position - block.transform.position);
            }
        }
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Optionally set Rigidbody2D back to dynamic if you want physics after drag
        /*
        foreach (Block b in draggingBlocks)
        {
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Dynamic;
        }
        */

        draggingBlocks = null;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        return cam.ScreenToWorldPoint(mousePos);
    }
}