using System.Collections.Generic;

[System.Serializable]
public class Player
{
    public string playerName;       
    public int position;           
    public int score;              
    public List<Card> hand = new(); 
    public bool isAI;               

    public Player(string name, int startPosition, bool isAI = false) 
    {
        this.playerName = name;
        this.position = startPosition; 
        this.score = 0;
        this.isAI = isAI;
    }
}