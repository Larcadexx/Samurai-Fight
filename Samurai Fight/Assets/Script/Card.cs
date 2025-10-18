using System;
using UnityEngine;

[Serializable]
public class Card
{
    // Nilai kartu (1–5)
    public int value;

    // Constructor — dipanggil saat membuat kartu baru
    public Card(int val)
    {
        this.value = val;
    }

    public override string ToString()
    {
        return $"Card({value})";
    }
}
