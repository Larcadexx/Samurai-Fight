using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SistemKartu : MonoBehaviour
{
    // Cukup hubungkan teks di Inspector, Image akan diambil secara otomatis
    public TextMeshProUGUI valueText;

    private Card cardData;
    private Image imageComponent; // Variabel untuk menyimpan komponen Image

    // Awake dipanggil sekali saat skrip diinisialisasi
    private void Awake()
    {
        // Secara otomatis mendapatkan komponen Image dari GameObject ini
        imageComponent = GetComponent<Image>();
    }
    
    /// <summary>
    /// Mengisi slot ini dengan data kartu dan menampilkannya.
    /// </summary>
    public void Initialize(Card data)
    {
        cardData = data;
        valueText.text = data.value.ToString();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Menyembunyikan slot kartu ini saat tidak digunakan.
    /// </summary>
    public void HideSlot()
    {
        cardData = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Fungsi ini dipanggil saat tombol kartu diklik.
    /// </summary>
    public void OnClick()
    {
        if (cardData == null) return;
        
        GameManager.instance.OnCardSlotClicked(cardData, this);
    }

    /// <summary>
    /// Mengubah warna Image untuk menandakan kartu sedang dipilih.
    /// </summary>
    public void ToggleSelection(bool select)
    {
        // Menggunakan imageComponent yang sudah disimpan di Awake
        if (imageComponent != null)
        {
            imageComponent.color = select ? Color.yellow : Color.white;
        }
    }
}