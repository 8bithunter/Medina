using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Variable : Block
{
    public float value;

    public Variable(string title, Color color, float value) 
        : base(title, color)
    {
        this.value = value;
    }
}
