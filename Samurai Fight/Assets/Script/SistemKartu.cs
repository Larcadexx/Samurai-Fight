using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistem slot kartu pada UI (mengikat data Card ke slot).
/// Tidak mengubah perilaku: Initialize, HideSlot, OnClick, ToggleSelection.
/// </summary>
public class SistemKartu : MonoBehaviour
{
    // Cukup hubungkan teks di Inspector, Image akan diambil secara otomatis
    public TextMeshProUGUI valueText;

    private Card cardData;
    private Image imageComponent; // menyimpan komponen Image

    private void Awake()
    {
        // Dapatkan komponen Image (jika ada)
        imageComponent = GetComponent<Image>();
    }

    /// <summary>
    /// Mengisi slot ini dengan data kartu dan menampilkannya.
    /// </summary>
    public void Initialize(Card data)
    {
        cardData = data;
        if (valueText != null)
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
    /// Dipanggil saat tombol kartu diklik (di-inspector biasanya terhubung ke Event).
    /// </summary>
    public void OnClick()
    {
        if (cardData == null) return;
        if (GameManager.instance == null) return;
        GameManager.instance.OnCardSlotClicked(cardData, this);
    }

    /// <summary>
    /// Mengubah warna Image untuk menandakan kartu sedang dipilih.
    /// </summary>
    public void ToggleSelection(bool select)
    {
        if (imageComponent != null)
            imageComponent.color = select ? Color.yellow : Color.white;
    }
}
