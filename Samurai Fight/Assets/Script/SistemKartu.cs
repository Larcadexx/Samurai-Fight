using UnityEngine;
using UnityEngine.UI; // Diperlukan untuk mengakses komponen Image dan Button
using TMPro; // Diperlukan untuk TextMeshProUGUI

// Skrip ini menempel pada setiap GameObject 'Slot Kartu' di UI
public class SistemKartu : MonoBehaviour
{
    // Cukup hubungkan teks di Inspector, Image akan diambil secara otomatis
    public TextMeshProUGUI valueText; // Referensi ke UI Text untuk menampilkan nilai kartu

    private Card cardData; // Variabel untuk menyimpan data 'Card' yang sedang ditampilkan slot ini
    private Image imageComponent; // Variabel untuk menyimpan komponen Image dari GameObject ini

    // Awake dipanggil sekali saat skrip diinisialisasi (bahkan sebelum Start)
    private void Awake()
    {
        // Secara otomatis mendapatkan komponen Image dari GameObject ini
        // Ini lebih efisien daripada GetComponent<Image>() berulang kali
        imageComponent = GetComponent<Image>();
    }
    
    /// Mengisi slot ini dengan data kartu dan menampilkannya.

    public void Initialize(Card data)
    {
        cardData = data; // Simpan data kartunya
        valueText.text = data.value.ToString(); // Tampilkan nilai kartu sebagai teks
        gameObject.SetActive(true); // Tampilkan GameObject slot kartu ini
    }

    /// Menyembunyikan slot kartu ini saat tidak digunakan.
    public void HideSlot()
    {
        cardData = null; // Hapus data kartu
        gameObject.SetActive(false); // Sembunyikan GameObject slot kartu
    }

    /// Fungsi ini dipanggil saat tombol kartu diklik.
    /// (Harus dihubungkan ke event OnClick() dari komponen Button di Inspector)
    public void OnClick()
    {
        // Jika slot ini tidak punya data (mungkin slot kosong), jangan lakukan apa-apa
        if (cardData == null) return; 
        
        // Memanggil fungsi di GameManager, mengirimkan data kartu yang diklik
        // dan referensi ke skrip 'SistemKartu' ini (untuk keperluan ToggleSelection)
        GameManager.instance.OnCardSlotClicked(cardData, this);
    }

    /// Mengubah warna Image untuk menandakan kartu sedang dipilih (saat menangkis).
    public void ToggleSelection(bool select)
    {
        // Menggunakan imageComponent yang sudah disimpan di Awake
        if (imageComponent != null)
        {
            // Jika 'select' true, ubah warna jadi kuning. Jika false, ubah jadi putih.
            imageComponent.color = select ? Color.yellow : Color.white;
        }
    }
}