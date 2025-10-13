using System.Collections.Generic;

// Kelas untuk menyimpan data tiap pemain (manusia atau AI)
[System.Serializable]
public class Player
{
    public string playerName;       // Nama pemain
    public int position;            // Posisi pion di papan
    public int score;               // Skor total
    public List<Card> hand = new(); // Kartu di tangan
    public bool isAI;               // True kalau pemain ini AI

    // Constructor
    public Player(string name, int startPos, bool isAI = false)
    {
        this.playerName = name;
        this.position = startPos;
        this.score = 0;
        this.isAI = isAI;
    }
}
