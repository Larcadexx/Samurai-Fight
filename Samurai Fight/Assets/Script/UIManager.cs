using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Asset Gambar Skor")]
    public Sprite scorePenuhSprite; // Sprite untuk skor yang terisi

    [Header("Score Display")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public GameObject cardPrefab;
    public Transform playerHandPanel;

    [Header("Decks")]
    public TextMeshProUGUI kartuDeckText;

    [Header("Panels")]
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;
    public GameObject AksiTangkisPanel;

    [Header("Action Buttons")]
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;

    void Awake() 
    { 
        instance = this; 
    }

    public void UpdateScoreUI(Player player)
    {
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;

        for (int i = 0; i < targetImages.Length; i++)
        {
            if (i < player.score)
            {
                targetImages[i].sprite = scorePenuhSprite;
            }
            else
            {
                targetImages[i].sprite = null;
            }
        }
    }

    public void ResetScoreUI()
    {
        foreach (Image img in scoreManusiaImages)
        {
            img.sprite = null;
        }
        foreach (Image img in scoreAiImages)
        {
            img.sprite = null;
        }
    }

    public void UpdateActionButtons(bool canMove, bool canAttack)
    {
        MelangkahButton.interactable = canMove;
        SerangButton.interactable = canAttack;
    }

    public void UpdateMoveDirectionButtons(bool canMoveForward, bool canMoveBackward)
    {
        MajuButton.interactable = canMoveForward;
        MundurButton.interactable = canMoveBackward;
    }

    public void SetPlayerHandInteractable(bool isInteractable)
    {
        foreach (Transform card in playerHandPanel)
        {
            card.GetComponent<Button>().interactable = isInteractable;
        }
    }
    
    public void ShowOpsiAwalPanel(bool show) 
    { 
        OpsiAwalPanel.SetActive(show); 
    }
    
    public void ShowAksiMelangkahPanel(bool show) 
    { 
        AksiMelangkahPanel.SetActive(show); 
    }
    
    public void ShowAksiTangkisPanel(bool show) 
    { 
        AksiTangkisPanel.SetActive(show); 
    }

    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
    }

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (Transform child in playerHandPanel) 
        { 
            Destroy(child.gameObject); 
        }
        foreach (Card card in player.hand)
        {
            GameObject cardGO = Instantiate(cardPrefab, playerHandPanel);
            cardGO.GetComponent<CardController>().Initialize(card);
        }
    }

    public void UpdateMainDeckUI(int cardCount)
    {
        if (cardCount > 0)
        {
            kartuDeckText.gameObject.SetActive(true);
            kartuDeckText.text = "Sisa: " + cardCount;
        }
        else 
        { 
            kartuDeckText.text = "Deck Habis!"; 
        }
    }
}