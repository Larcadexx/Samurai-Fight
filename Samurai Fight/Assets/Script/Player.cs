using System;
using System.Collections.Generic;

[Serializable]
public class Player
{
    public string playerName;  
    public int position;          
    public int score;              
    public List<Card> hand = new List<Card>(); 
    public bool isAI;               
    public Player(string name, int startPos, bool isAI = false)
    {
        this.playerName = name;
        this.position = startPos;
        this.score = 0;
        this.isAI = isAI;
        if (hand == null) hand = new List<Card>();
    }

    public override string ToString()
    {
        return $"{playerName} (pos:{position} score:{score} hand:{hand?.Count ?? 0})";
    }
}
