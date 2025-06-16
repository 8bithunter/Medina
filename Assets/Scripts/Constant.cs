using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constant : Block
{
    private float value;

    public Constant(string title, Color color, float value) 
        : base(title, color)
    {
        this.value = value;
    }
}
