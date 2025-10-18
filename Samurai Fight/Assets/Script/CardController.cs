using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        GameManager.instance.OnCardInHandClicked(cardData, this);
    }

    public void ToggleSelection(bool select)
    {
        isSelected = select;
        background.color = isSelected ? Color.yellow : Color.white;
    }
}
