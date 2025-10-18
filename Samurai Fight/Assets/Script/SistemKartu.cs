using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SistemKartu : MonoBehaviour
{

    public TextMeshProUGUI valueText;

    private Card cardData;
    private Image imageComponent; 

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    public void Initialize(Card data)
    {
        cardData = data;
        if (valueText != null)
            valueText.text = data.value.ToString();

        gameObject.SetActive(true);
    }

    public void HideSlot()
    {
        cardData = null;
        gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if (cardData == null) return;
        if (GameManager.instance == null) return;
        GameManager.instance.OnCardSlotClicked(cardData, this);
    }

    public void ToggleSelection(bool select)
    {
        if (imageComponent != null)
            imageComponent.color = select ? Color.yellow : Color.white;
    }
}
