// CardController.cs
using UnityEngine;
using TMPro;

// Script ini mengontrol perilaku setiap objek kartu yang ada di tangan pemain.
public class CardController : MonoBehaviour
{
    // Menyimpan data kartu (nilai) yang dipegang oleh objek ini.
    private Card cardData;
    // Referensi ke komponen TextMeshPro untuk menampilkan nilai kartu.
    public TextMeshProUGUI valueCardText;

    // Fungsi untuk inisialisasi, dipanggil oleh UIManager setelah kartu dibuat.
    public void Initialize(Card data)
    {
        // Menyimpan data kartu yang diterima.
        cardData = data;
        // Menampilkan nilai kartu ke UI Text.
        valueCardText.text = data.value.ToString();
    }

    // Fungsi yang dipanggil saat objek kartu ini diklik oleh pemain.
    public void OnClick()
    {
        // Jika karena suatu hal kartu ini tidak punya data, hentikan fungsi.
        if (cardData == null) return;
        
        // Memanggil fungsi di GameManager dan mengirimkan data kartu yang diklik.
        GameManager.instance.OnCardInHandClicked(cardData); 
    }
}