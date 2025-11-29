using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 

public class SistemKartu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI valueText;
    private float highlightYOffset = 35f;     
    private float highlightScaleMultiplier = 1.1f; 
    private Card kartuData;
    private Image imageComponent;
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private bool isSelected = false;
    private bool isHovered = false;

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
        originalPosition = transform.localPosition; 
        originalScale = transform.localScale;
    }

    public void Initialize(Card data)
    {
        kartuData = data;
        valueText.text = data.value.ToString();
        gameObject.SetActive(true);

        isSelected = false;
        isHovered = false;
        UpdateVisualState(); 
    }

    public void HideSlot()
    {
        kartuData = null;
        gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if (kartuData == null) return;
        GameManager.instance.OnKartuHandPressed(kartuData, this);
    }

    public void ToggleSelection(bool select)
    {
        isSelected = select;
        UpdateVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
    }


    private void UpdateVisualState()
    {
        if (imageComponent == null) return;
        if (isSelected || isHovered)
        {
            transform.localPosition = originalPosition + new Vector3(0, highlightYOffset, 0);
            
            transform.localScale = originalScale * highlightScaleMultiplier;
        }
        else
        {
            transform.localPosition = originalPosition;
            transform.localScale = originalScale;
        }
    }
}