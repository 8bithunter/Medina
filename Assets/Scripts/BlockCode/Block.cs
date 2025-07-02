using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public string title;
    public Color color;
    public Block(string title, Color color)
    {
        this.title = title;
        this.color = color;
    }

    void Start()
    {
        CallEveryFrame();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Freezes Z rotation
    }

    void Update()
    {
        CallEveryFrame();
    }

    public void CallEveryFrame()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }



    public List<Block> connectedAbove = new List<Block>();
    public List<Block> connectedBelow = new List<Block>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Block other = collision.gameObject.GetComponent<Block>();
        if (other == null) return;

        Vector3 myPos = transform.position;
        Vector3 otherPos = other.transform.position;

        float dx = Mathf.Abs(myPos.x - otherPos.x);
        float dy = otherPos.y - myPos.y;

        if (dx < 0.2f)  // Horizontally aligned
        {
            if (dy > 0.5f && dy < 1.5f)
            {
                // Other is above me
                other.Snap(this, above: false); // This goes below
            }
            else if (dy < -0.5f && dy > -1.5f)
            {
                // Other is below me
                other.Snap(this, above: true); // This goes above
            }
        }
    }

    public void Snap(Block other, bool above)
    {
        Vector3 newPos;
        if (above)
        {
            newPos = new Vector3(other.transform.position.x, other.transform.position.y + 1, transform.position.z);

            // this block is above other
            if (!other.connectedAbove.Contains(this)) other.connectedAbove.Add(this);
            if (!connectedBelow.Contains(other)) connectedBelow.Add(other);
        }
        else
        {
            newPos = new Vector3(other.transform.position.x, other.transform.position.y - 1, transform.position.z);

            // this block is below other
            if (!other.connectedBelow.Contains(this)) other.connectedBelow.Add(this);
            if (!connectedAbove.Contains(other)) connectedAbove.Add(other);
        }
        transform.position = newPos;
    }

    public void DisconnectAllConnections()
    {
        // Remove self from all connectedAbove blocks' connectedBelow lists
        foreach (Block above in connectedAbove)
        {
            above.connectedBelow.Remove(this);
        }
        connectedAbove.Clear();

        // Remove self from all connectedBelow blocks' connectedAbove lists
        foreach (Block below in connectedBelow)
        {
            below.connectedAbove.Remove(this);
        }
        connectedBelow.Clear();
    }

    public HashSet<Block> GetConnectedBlocksRecursive()
    {
        HashSet<Block> visited = new HashSet<Block>();
        CollectRecursive(this, visited);
        return visited;
    }

    private void CollectRecursive(Block b, HashSet<Block> visited)
    {
        if (visited.Contains(b)) return;
        visited.Add(b);

        foreach (Block a in b.connectedAbove)
            CollectRecursive(a, visited);
        foreach (Block d in b.connectedBelow)
            CollectRecursive(d, visited);
    }

    public HashSet<Block> GetBlocksBelowRecursive()
    {
        HashSet<Block> visited = new HashSet<Block>();
        CollectBelow(this, visited);
        return visited;
    }

    private void CollectBelow(Block current, HashSet<Block> visited)
    {
        if (visited.Contains(current)) return;
        Debug.Log($"Adding block {current.name}");
        visited.Add(current);

        foreach (Block below in current.connectedBelow)
        {
            CollectBelow(below, visited);
        }
    }
}
