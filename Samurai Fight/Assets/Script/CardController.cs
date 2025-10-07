using UnityEngine;
using TMPro;
using UnityEngine.UI; // <-- Tambahkan ini untuk mengakses komponen Image

public class CardController : MonoBehaviour
{
    private Card cardData;
    public TextMeshProUGUI valueCardText;
    
    public Image background; 
    private bool isSelected = false;

    public void Initialize(Card data)
    {
        cardData = data;
        valueCardText.text = data.value.ToString();
    }
    
    public void OnClick()
    {
        if (cardData == null) return;
        
        // Teruskan data kartu dan informasi tentang CardController ini ke GameManager
        GameManager.instance.OnCardInHandClicked(cardData, this); 
    }

    public void ToggleSelection(bool select)
    {
        isSelected = select;
        if (isSelected)
        {
            background.color = Color.yellow;
        }
        else
        {
            background.color = Color.white;
        }
    }
}