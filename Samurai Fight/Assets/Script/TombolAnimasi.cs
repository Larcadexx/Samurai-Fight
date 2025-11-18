using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections; 

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))] 
public class TombolAnimasi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Pengaturan Animasi")]
    public float scaleHover = 1.1f;
    public float scaleTekan = 0.95f;
    public float posisiHoverY = 10f; 
    public float durasiAnimasi = 0.15f; 

    [Header("Pengaturan Audio")]
    public AudioClip audioKlik; 

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Button myButton;
    private AudioSource audioSource; 
    private Coroutine animasiBerjalan;

    void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        myButton = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myButton != null && myButton.interactable)
        {
            Vector3 targetScale = originalScale * scaleHover;
            Vector3 targetPos = originalPosition + new Vector3(0, posisiHoverY, 0);
            
            StartAnimasi(targetScale, targetPos);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartAnimasi(originalScale, originalPosition);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (myButton != null && myButton.interactable)
        {
            Vector3 targetScale = originalScale * scaleTekan;
            Vector3 targetPos = originalPosition + new Vector3(0, posisiHoverY, 0);
            
            StartAnimasi(targetScale, targetPos);
            
            if (audioKlik != null)
            {
                audioSource.PlayOneShot(audioKlik);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (myButton != null && myButton.interactable)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
            {
                Vector3 targetScale = originalScale * scaleHover;
                Vector3 targetPos = originalPosition + new Vector3(0, posisiHoverY, 0);
                
                StartAnimasi(targetScale, targetPos);
            }
            else
            {
                StartAnimasi(originalScale, originalPosition);
            }
        }
    }

    private void StartAnimasi(Vector3 targetScale, Vector3 targetPosition)
    {
        if (animasiBerjalan != null)
        {
            StopCoroutine(animasiBerjalan);
        }
        animasiBerjalan = StartCoroutine(Animasi(targetScale, targetPosition));
    }

    private IEnumerator Animasi(Vector3 targetScale, Vector3 targetPosition)
    {
        float waktu = 0;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.localPosition;

        while (waktu < durasiAnimasi)
        {
            float t = waktu / durasiAnimasi;
            t = t * t * (3f - 2f * t); 
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            transform.localPosition = Vector3.Lerp(startPos, targetPosition, t);
            waktu += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        transform.localPosition = targetPosition;
        animasiBerjalan = null;
    }

    void OnDisable()
    {
        if (animasiBerjalan != null)
        {
            StopCoroutine(animasiBerjalan);
        }
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
    }
}