using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Pengaturan Gerakan")]
    public float jarak = 10f; 
    public float kecepatan = 5f; 

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (rectTransform != null)
        {
            startPos = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        float newY = startPos.y + Mathf.Sin(Time.time * kecepatan) * jarak;
        
        rectTransform.anchoredPosition = new Vector2(startPos.x, newY);
    }
}