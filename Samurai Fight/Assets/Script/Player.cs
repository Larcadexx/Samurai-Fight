using System.Collections.Generic;

[System.Serializable]
public class Player
{
    public string playerName;
    public int position;
    public int score;
    public Card[] hand = new Card[5]; 
    public bool isAI;

    public Player(string name, int startPos, bool isAI = false)
    {
        this.playerName = name;
        this.position = startPos;
        this.score = 0;
        this.isAI = isAI;
    }
}