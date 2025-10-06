[System.Serializable] // Membuat data kelas ini bisa dilihat di Unity Inspector.
public class Card
{
    // Variabel untuk menyimpan nilai kartu (misalnya: 1, 2, 3, 4, atau 5).
    public int value;

    // Constructor: Fungsi yang dipanggil saat sebuah objek Card baru dibuat.
    public Card(int val)
    {
        // 'this.value' menunjuk ke variabel 'value' di dalam kelas ini.
        // 'val' adalah nilai yang dikirim saat membuat kartu baru.
        this.value = val;
    }
}