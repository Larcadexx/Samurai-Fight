using System;

// Script sederhana untuk menyimpan data kartu
[System.Serializable]
public class Card
{
    // Nilai kartu (1–5)
    public int value;

    // Constructor — dipanggil saat membuat kartu baru
    public Card(int value) // Perubahan: val -> value
    {
        this.value = value; // Simpan nilai ke dalam variabel di kelas
    }
}