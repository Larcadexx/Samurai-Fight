using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 

public class CardSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI valueText;

    [Header("Visual Settings")]
    public float smoothSpeed = 15f;
    
    [Header("Hover State")]
    public float hoverYOffset = 30f;
    public float hoverScale = 1.15f;

    [Header("Selected State")]
    public float selectYOffset = 70f;
    public float selectScale = 1.25f;
    
    public Color selectedColor = new Color(1f, 0.8f, 0f, 1f); 

    private Card cardData;
    private Image imageComponent;
    
    private Canvas cardCanvas;
    private GraphicRaycaster cardRaycaster;

    // Default values
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;

    private Vector3 targetPosition;
    private Vector3 targetScale;
    private Color targetColor;

    private bool isSelected = false;
    private bool isHovered = false;
    private bool isInteractable = true; 
    
    private bool hasInitializedDefaults = false;

    private void Awake()
    {
        SetupDefaults();
    }

    private void SetupDefaults()
    {
        if (hasInitializedDefaults) return;

        imageComponent = GetComponent<Image>();
        
        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null) cardCanvas = gameObject.AddComponent<Canvas>();
        
        cardRaycaster = GetComponent<GraphicRaycaster>();
        if (cardRaycaster == null) cardRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 0;
        
        if (transform.localScale != Vector3.zero)
        {
            originalPosition = transform.localPosition; 
            originalScale = transform.localScale;
        }
        else
        {
            originalPosition = transform.localPosition;
            originalScale = Vector3.one; 
        }

        if (imageComponent != null) originalColor = imageComponent.color;

        targetPosition = originalPosition;
        targetScale = originalScale;
        targetColor = originalColor;

        hasInitializedDefaults = true;
    }

    private void Update()
    {
        if (!hasInitializedDefaults) return;

        // Lerp Movement
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);

        if (imageComponent != null)
        {
            imageComponent.color = Color.Lerp(imageComponent.color, targetColor, Time.deltaTime * smoothSpeed);
        }
    }

    public void Initialize(Card data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        SetupDefaults();
        cardData = data;
        
        if (valueText != null) valueText.text = data.value.ToString();

        gameObject.SetActive(true);

        isSelected = false;
        isHovered = false;
        isInteractable = true;
        
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
        if(imageComponent != null) imageComponent.color = originalColor;
        
        if (cardCanvas != null) cardCanvas.sortingOrder = 0;

        targetPosition = originalPosition;
        targetScale = originalScale;
        targetColor = originalColor;

        UpdateVisualTargets(); 
    }

    public void SetInteractable(bool status)
    {
        isInteractable = status;
        if (!status)
        {
            isHovered = false;
            UpdateVisualTargets();
        }
    }

    public void HideSlot()
    {
        cardData = null;
        isSelected = false;
        isHovered = false;
        if (cardCanvas != null) cardCanvas.sortingOrder = 0;
        gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if (!isInteractable || cardData == null) return;
        GameManager.instance.OnCardHandPressed(cardData, this);
    }

    public void ToggleSelection(bool select)
    {
        isSelected = select;
        UpdateVisualTargets();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        isHovered = true;
        UpdateVisualTargets();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualTargets();
    }

    private void UpdateVisualTargets()
    {
        if (!hasInitializedDefaults) SetupDefaults();

        if (cardCanvas != null)
        {
            if (isSelected || isHovered)
            {
                cardCanvas.overrideSorting = true;
                cardCanvas.sortingOrder = 10; 
            }
            else
            {
                cardCanvas.overrideSorting = false;
                cardCanvas.sortingOrder = 0;
            }
        }

        if (!isInteractable)
        {
            if (isSelected) SetTargetToSelected();
            else SetTargetToNormal();
            return;
        }

        if (isSelected)
        {
            SetTargetToSelected();
        }
        else if (isHovered)
        {
            targetPosition = originalPosition + new Vector3(0, hoverYOffset, 0);
            targetScale = originalScale * hoverScale;
            targetColor = originalColor;
        }
        else
        {
            SetTargetToNormal();
        }
    }

    private void SetTargetToSelected()
    {
        targetPosition = originalPosition + new Vector3(0, selectYOffset, 0);
        targetScale = originalScale * selectScale;
        targetColor = selectedColor; 
    }

    private void SetTargetToNormal()
    {
        targetPosition = originalPosition;
        targetScale = originalScale;
        targetColor = originalColor;
    }
}