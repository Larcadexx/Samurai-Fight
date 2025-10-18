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

    public void OnClick()
    {
        if (cardData == null) return;
        GameManager.instance.OnCardInHandClicked(cardData); 
    }
}