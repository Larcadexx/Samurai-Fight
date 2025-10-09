// Player.cs
using System.Collections.Generic;

[System.Serializable] // Membuat data kelas ini bisa dilihat di Unity Inspector.
public class Player
{
    // Variabel untuk nama pemain (misal: "Manusia" atau "AI").
    public string playerName;
    // Variabel untuk menyimpan posisi pion pemain di papan permainan.
    public int position;
    // Variabel untuk menyimpan skor pemain.
    public int score;
    // List (daftar) untuk menyimpan kartu-kartu yang ada di tangan pemain.
    public List<Card> hand = new List<Card>();
    // Penanda boolean, 'true' jika pemain ini adalah AI.
    public bool isAI;

    // Constructor: Fungsi yang dipanggil saat sebuah objek Player baru dibuat.
    public Player(string name, int startPos, bool isAI = false)
    {
        // Mengisi data pemain berdasarkan parameter yang dikirim.
        this.playerName = name;
        this.position = startPos;
        this.score = 0;
        this.isAI = isAI;
    }
}