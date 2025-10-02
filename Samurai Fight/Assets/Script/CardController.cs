// CardController.cs
using UnityEngine;
using TMPro;

// Script ini adalah "otak" untuk setiap prefab kartu di tangan pemain.
public class CardController : MonoBehaviour
{
    private Card cardData;
    public TextMeshProUGUI valueCardText;

    // Fungsi ini menerima data kartu dari UIManager, lalu menampilkan nilainya.
    public void Initialize(Card data)
    {
        cardData = data;
        valueCardText.text = data.value.ToString();
    }

    // Fungsi ini akan dipanggil saat kartu di-klik (saat ini dinonaktifkan).
    public void OnClick()
    {
        if (cardData == null) return;
        // GameManager.instance.OnCardClicked(cardData);
    }
}