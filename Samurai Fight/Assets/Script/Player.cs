using System.Collections.Generic; // Diperlukan untuk menggunakan List<>

// Kelas untuk menyimpan data tiap pemain (manusia atau AI)
[System.Serializable] // Agar bisa terlihat di Inspector Unity
public class Player
{
    public string playerName;       // Nama pemain (cth: "Manusia" or "AI")
    public int position;            // Posisi pion di papan (petak ke-1, 2, dst.)
    public int score;               // Skor total (berapa ronde dimenangkan)
    public List<Card> hand = new(); // Kartu di tangan. Diinisialisasi sebagai List baru.
    public bool isAI;               // True kalau pemain ini AI, false kalau manusia

    // Constructor - dipanggil saat membuat pemain baru
    // Contoh: new Player("Manusia", 1) atau new Player("AI", 23, true)
    public Player(string name, int startPos, bool isAI = false) // 'isAI = false' adalah nilai default
    {
        this.playerName = name;     // Set nama
        this.position = startPos;   // Set posisi awal
        this.score = 0;             // Skor selalu mulai dari 0
        this.isAI = isAI;           // Set apakah ini AI atau bukan
    }
}