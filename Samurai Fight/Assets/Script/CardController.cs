// CardController.cs
using UnityEngine;
using TMPro;

public class CardController : MonoBehaviour
{
    private Card cardData;
    public TextMeshProUGUI valueCardText;

    public void Initialize(Card data)
    {
        cardData = data;
        valueCardText.text = data.value.ToString();
    }

    // Fungsi OnClick sekarang aktif dan memanggil GameManager
    public void OnClick()
    {
        if (cardData == null) return;
        
        // Panggil fungsi di GameManager dan kirim data kartu ini
        GameManager.instance.OnCardInHandClicked(cardData); 
    }
}