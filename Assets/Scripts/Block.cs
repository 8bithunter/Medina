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
    }

    void Update()
    {
        CallEveryFrame();    }

    public void CallEveryFrame()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }
}
