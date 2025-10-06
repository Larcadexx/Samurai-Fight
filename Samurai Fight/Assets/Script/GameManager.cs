// GameManager.cs

// Mengimpor library yang dibutuhkan.
// System.Collections.Generic untuk menggunakan List dan Dictionary.
using System.Collections.Generic;
// UnityEngine untuk mengakses fungsionalitas engine Unity.
using UnityEngine;
// System.Text untuk menggunakan kelas StringBuilder yang efisien.
using System.Text;


// Ini adalah kelas utama yang menjadi pusat kendali dari seluruh logika permainan.
public class GameManager : MonoBehaviour
{
    // 'instance' adalah variabel statis untuk menerapkan Singleton Pattern.
    // Ini memastikan hanya ada satu GameManager dan membuatnya mudah diakses dari skrip lain.
    public static GameManager instance;

    // Referensi publik ke skrip UIManager untuk mengontrol semua elemen UI.
    // Variabel ini akan diisi melalui Unity Inspector.
    public UIManager uiManager;


    // Variabel privat untuk menyimpan data pemain manusia. Dibuat dari kelas Player.
    private Player manusia;
    // Variabel privat untuk menyimpan data pemain AI. Dibuat dari kelas Player.
    private Player ai;


    // List<Card> ini berfungsi sebagai dek utama tempat semua kartu permainan disimpan saat ronde dimulai.
    private List<Card> deck = new List<Card>();


    // --- Variabel Status untuk Melacak Aksi Pemain ---
    // Variabel boolean yang menjadi 'true' hanya jika pemain sedang dalam proses memilih kartu untuk melangkah.
    private bool isPlayerMelangkah = false;
    // Variabel integer untuk menyimpan arah langkah yang dipilih: 1 untuk Maju, -1 untuk Mundur.
    private int arahLangkah = 0;


    // Awake() adalah fungsi bawaan Unity yang dipanggil paling pertama saat skrip dimuat.
    // Digunakan di sini untuk mengatur 'instance' singleton agar siap digunakan oleh skrip lain.
    void Awake() { instance = this; }


    // Start() adalah fungsi bawaan Unity yang dipanggil sekali pada frame pertama, setelah Awake().
    // Digunakan untuk memulai urutan logika utama permainan.
    void Start()
    {
        // Memanggil fungsi untuk setup awal data pemain.
        SetupGame();
        // Memanggil fungsi untuk memulai ronde pertama permainan.
        StartRound();
    }


    // Fungsi ini hanya dijalankan sekali di awal untuk menciptakan objek pemain dan mengatur data awalnya.
    void SetupGame()
    {
        // Membuat objek 'manusia' baru dengan nama "Manusia" dan posisi awal di petak 1.
        manusia = new Player("Manusia", 1);
        // Membuat objek 'ai' baru dengan nama "AI", posisi awal 23, dan menandainya sebagai AI (true).
        ai = new Player("AI", 23, true);
    }


    // Fungsi ini mengatur semua yang diperlukan untuk memulai sebuah ronde baru.
    public void StartRound()
    {
        // Memanggil fungsi untuk membuat dek baru berisi 25 kartu standar.
        CreateDeck();
        // Memanggil fungsi untuk mengacak urutan kartu di dalam dek.
        ShuffleDeck();

        // Mengosongkan kartu di tangan pemain dari ronde sebelumnya untuk memastikan tangan bersih.
        manusia.hand.Clear();
        ai.hand.Clear();
        // Loop ini berjalan 5 kali untuk membagikan kartu kepada setiap pemain.
        for (int i = 0; i < 5; i++)
        {
            // Mengambil satu kartu dari dek dan menambahkannya ke tangan manusia.
            manusia.hand.Add(DrawCardFromDeck());
            // Mengambil satu kartu lagi dari dek dan menambahkannya ke tangan AI.
            ai.hand.Add(DrawCardFromDeck());
        }

        // Memerintahkan UIManager untuk memperbarui tampilan visual kartu di tangan pemain.
        uiManager.UpdatePlayerHandUI(manusia);
        // Memerintahkan UIManager untuk memperbarui teks jumlah sisa kartu di dek.
        uiManager.UpdateMainDeckUI(deck.Count);
        // Memanggil fungsi logging untuk keperluan debugging (melihat status di konsol Unity).
        LogDeckStock();
        LogPlayerHand(manusia);
        LogPlayerHand(ai);

        // Menampilkan panel pilihan aksi awal kepada pemain untuk memulai giliran.
        uiManager.ShowOpsiAwalPanel(true);
    }


    // --- FUNGSI-FUNGSI UNTUK AKSI PEMAIN ---

    // Fungsi ini dipanggil dari event OnClick pada tombol "Melangkah" di UI.
    public void OnMelangkahButtonPressed()
    {
        // Menyembunyikan panel opsi awal ("Melangkah" / "Serang").
        uiManager.ShowOpsiAwalPanel(false);
        // Menampilkan panel pilihan arah ("Maju" / "Mundur").
        uiManager.ShowAksiMelangkahPanel(true);
    }

    // Fungsi ini dipanggil dari event OnClick pada tombol "Maju" atau "Mundur" di UI.
    public void OnArahLangkahPressed(bool isMaju)
    {
        // Mengatur status bahwa pemain sekarang siap memilih kartu untuk dieksekusi sebagai langkah.
        isPlayerMelangkah = true;
        // Menggunakan operator ternary untuk menentukan nilai arahLangkah secara ringkas.
        // Jika isMaju true, arahLangkah menjadi 1. Jika false, arahLangkah menjadi -1.
        arahLangkah = isMaju ? 1 : -1;

        // Memberi pesan di konsol sebagai panduan untuk developer atau pemain saat testing.
        Debug.Log("Silakan pilih kartu untuk melangkah.");
        // Menyembunyikan panel pilihan arah setelah arah dipilih.
        uiManager.ShowAksiMelangkahPanel(false);
    }

    // Fungsi ini dipanggil dari CardController setiap kali sebuah objek kartu di tangan diklik.
    public void OnCardInHandClicked(Card clickedCard)
    {
        // Cek terlebih dahulu apakah pemain memang sedang dalam fase melangkah.
        if (isPlayerMelangkah)
        {
            // Menghitung calon posisi baru pemain berdasarkan nilai kartu dan arah yang sudah disimpan.
            int newPosition = manusia.position + (clickedCard.value * arahLangkah);

            // Melakukan validasi langkah sesuai aturan permainan.
            // Langkah maju (arahLangkah == 1) valid jika posisi baru berada sebelum posisi AI.
            // Langkah mundur (arahLangkah == -1) valid jika posisi baru tidak kurang dari 1 (batas papan).
            bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

            // Jika hasil validasi adalah true...
            if (isValidMove)
            {
                // Perbarui posisi pemain ke posisi yang baru.
                manusia.position = newPosition;
                Debug.Log($"Pemain melangkah ke posisi: {manusia.position}");

                // Hapus kartu yang telah digunakan dari List di tangan pemain.
                manusia.hand.Remove(clickedCard);

                // Reset variabel status agar siap untuk aksi berikutnya dan mencegah aksi ganda.
                isPlayerMelangkah = false;
                arahLangkah = 0;

                // Perbarui tampilan UI kartu di tangan pemain setelah satu kartu dihapus.
                uiManager.UpdatePlayerHandUI(manusia);

                // TODO: Logika selanjutnya adalah pindah ke fase "Sergap".
            }
            // Jika hasil validasi adalah false...
            else
            {
                // Beri pesan error di konsol untuk memberitahu pemain kenapa langkahnya gagal.
                Debug.Log("Langkah tidak valid! Anda tidak bisa melangkah melewati lawan atau keluar papan.");
                // Reset status dan kembalikan pemain ke panel pilihan aksi awal.
                isPlayerMelangkah = false;
                uiManager.ShowOpsiAwalPanel(true);
            }
        }
        // Jika pemain mengklik kartu di saat yang tidak tepat (bukan saat fase melangkah)...
        else
        {
            Debug.Log("Pilih aksi dulu (Melangkah / Serang).");
        }
    }


    // --- FUNGSI-FUNGSI MANAJEMEN DEK & LOGGING ---

    // Fungsi untuk membuat dek baru berisi 25 kartu (5 kartu untuk setiap nilai dari 1 sampai 5).
    private void CreateDeck()
    {
        // Mengosongkan dek untuk memastikan tidak ada kartu sisa dari ronde sebelumnya.
        deck.Clear();
        // Loop luar untuk nilai kartu, dari 1 hingga 5.
        for (int value = 1; value <= 5; value++)
        {
            // Loop dalam untuk membuat 5 buah kartu untuk setiap nilai yang ditentukan loop luar.
            for (int count = 0; count < 5; count++)
            {
                // Menambahkan objek kartu baru dengan nilai yang sesuai ke dalam List dek.
                deck.Add(new Card(value));
            }
        }
    }

    // Fungsi untuk mencatat jumlah setiap nilai kartu yang tersisa di dek (untuk debugging).
    private void LogDeckStock()
    {
        // Menggunakan Dictionary untuk menghitung jumlah setiap nilai kartu.
        Dictionary<int, int> sisaStok = new Dictionary<int, int>();
        foreach (Card card in deck)
        {
            if (!sisaStok.ContainsKey(card.value)) { sisaStok[card.value] = 0; }
            sisaStok[card.value]++;
        }

        // Menggunakan StringBuilder untuk membuat string log secara efisien.
        StringBuilder logStok = new StringBuilder("Sisa Stok Kartu di deck : ");
        for (int i = 1; i <= 5; i++)
        {
            int jumlah = sisaStok.ContainsKey(i) ? sisaStok[i] : 0;
            logStok.Append($"{i}={jumlah} ");
        }
        // Menampilkan hasil string ke konsol Unity.
        Debug.Log(logStok.ToString());
    }

    // Fungsi untuk mengacak urutan kartu di dalam dek menggunakan algoritma Fisher-Yates.
    private void ShuffleDeck()
    {
        // Loop dari kartu pertama hingga terakhir.
        for (int i = 0; i < deck.Count; i++)
        {
            // Simpan kartu saat ini.
            Card temp = deck[i];
            // Pilih kartu acak dari posisi saat ini hingga akhir dek.
            int randomIndex = Random.Range(i, deck.Count);
            // Tukar posisi kartu saat ini dengan kartu acak.
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // Fungsi untuk mengambil kartu teratas dari dek.
    private Card DrawCardFromDeck()
    {
        // Pengaman jika dek sudah habis, kembalikan null untuk menghindari error.
        if (deck.Count == 0) return null;
        // Ambil kartu di posisi paling atas (indeks 0).
        Card drawnCard = deck[0];
        // Hapus kartu tersebut dari dek.
        deck.RemoveAt(0);
        // Kembalikan kartu yang sudah diambil.
        return drawnCard;
    }

    // Fungsi untuk mencatat kartu yang ada di tangan pemain ke konsol (untuk debugging).
    private void LogPlayerHand(Player player)
    {
        // Menggunakan StringBuilder untuk efisiensi.
        StringBuilder logTangan = new StringBuilder($"{player.playerName} : ");
        // Loop melalui setiap kartu di tangan pemain.
        foreach (Card card in player.hand)
        {
            // Tambahkan nilai kartu ke string.
            logTangan.Append(card.value + " ");
        }
        // Tampilkan hasilnya ke konsol.
        Debug.Log(logTangan.ToString());
    }
}