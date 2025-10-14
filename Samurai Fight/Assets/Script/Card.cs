using System; // Diperlukan untuk [System.Serializable]

// [System.Serializable] memungkinkan Unity untuk menampilkan kelas ini di Inspector
// (misalnya, di dalam List<Card> milik Player)
[System.Serializable]
public class Card
{
    // Nilai kartu (1–5)
    public int value; 

    // Constructor — fungsi khusus yang dipanggil saat membuat kartu baru
    // Contoh: new Card(5)
    public Card(int val)
    {
        // 'this.value' merujuk ke variabel 'value' milik kelas ini
        // 'val' adalah parameter yang dikirim saat membuat kartu
        this.value = val; // Simpan nilai ke dalam variabel di kelas
    }
}