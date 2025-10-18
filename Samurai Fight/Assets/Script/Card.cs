using System;
using UnityEngine;

[Serializable]
public class Card
{
    public int value;

    public Card(int val)
    {
        this.value = val;
    }

    public override string ToString()
    {
        return $"Card({value})";
    }
}
