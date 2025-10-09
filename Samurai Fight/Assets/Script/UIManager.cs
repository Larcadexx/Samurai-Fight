using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Position Markers")]
    public Transform handPanelDefaultMarker;
    public Transform handPanelSelectionMarker;
    public Transform handPanelAttackMarker;

    [Header("Persistent UI")]
    public GameObject[] persistentUIPanels;

    [Header("Asset Gambar Skor")]
    public Sprite scorePenuhSprite;

    [Header("Score Display")]
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public GameObject cardPrefab;
    public Transform KartuManusiaPanel; 

    [Header("Decks")]
    public TextMeshProUGUI kartuDeckText;

    [Header("Info Tambahan")]
    public TextMeshProUGUI NilaiSerangan;
    public TextMeshProUGUI SystemMessage;
    public TextMeshProUGUI KemenanganText;

    [Header("Panels")]
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;
    public GameObject AksiTangkisPanel;
    public GameObject AksiSergapPanel;
    public GameObject KemenanganPanel;

    [Header("Action Buttons")]
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;

    private Coroutine messageCoroutine;

    void Awake() 
    { 
        instance = this; 
        if (NilaiSerangan != null) NilaiSerangan.gameObject.SetActive(false);
        if (SystemMessage != null) SystemMessage.gameObject.SetActive(false);
        if (KemenanganPanel != null) KemenanganPanel.SetActive(false);
    }
    
    public void ShowKemenanganPanel(bool playerWon)
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.gameObject.SetActive(false);
        HideMessage();
        HideAttackStrength();

        KemenanganPanel.SetActive(true);

        if (playerWon)
        {
            KemenanganText.text = "ANDA MENANG!";
        }
        else
        {
            KemenanganText.text = "ANDA KALAH!";
        }
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(true);
        if (KartuManusiaPanel != null && handPanelDefaultMarker != null)
        {
            KartuManusiaPanel.position = handPanelDefaultMarker.position;
        }
    }

    private void SetPersistentUIVisibility(bool isVisible)
    {
        foreach(var panel in persistentUIPanels)
        {
            if (panel != null)
            {
                panel.SetActive(isVisible);
            }
        }
    }

    public void ShowOpsiAwalPanel(bool show) 
    { 
        OpsiAwalPanel.SetActive(show);
        if (show)
        {
            SetPersistentUIVisibility(true);
            if (KartuManusiaPanel != null && handPanelDefaultMarker != null)
            {
                KartuManusiaPanel.position = handPanelDefaultMarker.position;
            }
        }
    }

    public void ShowAksiMelangkahPanel(bool show) 
    { 
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.gameObject.SetActive(false);
        
        AksiMelangkahPanel.SetActive(show);
    }
    
    public void ShowAksiTangkisPanel(bool show) 
    { 
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.gameObject.SetActive(false);

        AksiTangkisPanel.SetActive(show);
    }

    public void ShowAksiSergapPanel(bool show)
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.gameObject.SetActive(false);

        AksiSergapPanel.SetActive(show);
    }

    public void ShowHandPanelOnly(string context)
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);

        if (KartuManusiaPanel != null)
        {
            KartuManusiaPanel.gameObject.SetActive(true);
            
            if (context == "attack" && handPanelAttackMarker != null)
            {
                KartuManusiaPanel.position = handPanelAttackMarker.position;
            }
            else if (context == "move" && handPanelSelectionMarker != null)
            {
                KartuManusiaPanel.position = handPanelSelectionMarker.position;
            }
        }
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (SystemMessage == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        SystemMessage.text = message;
        SystemMessage.gameObject.SetActive(true);

        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            SystemMessage.gameObject.SetActive(false);
        }
    }

    public void HideMessage()
    {
        if (SystemMessage == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        SystemMessage.gameObject.SetActive(false);
    }

    public void ShowAttackStrength(int strength)
    {
        if (NilaiSerangan == null) return;
        NilaiSerangan.gameObject.SetActive(true);
        NilaiSerangan.text = $"KEKUATAN SERANGAN: {strength}";
    }

    public void UpdateSelectedParryTotal(int total)
    {
        if (NilaiSerangan == null) return;
        NilaiSerangan.gameObject.SetActive(true);
        NilaiSerangan.text = $"TOTAL TANGKISAN: {total}";
    }

    public void HideAttackStrength()
    {
        if (NilaiSerangan == null) return;
        NilaiSerangan.gameObject.SetActive(false);
    }

    public void UpdatePlayerHandUI(Player player)
    {
        foreach (Transform child in KartuManusiaPanel) { Destroy(child.gameObject); }
        foreach (Card card in player.hand)
        {
            GameObject cardGO = Instantiate(cardPrefab, KartuManusiaPanel);
            cardGO.GetComponent<CardController>().Initialize(card);
        }
    }

    public void SetPlayerHandInteractable(bool isInteractable)
    {
        foreach (Transform card in KartuManusiaPanel)
        {
            card.GetComponent<Button>().interactable = isInteractable;
        }
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

    public void HideAllPlayerPanels()
    {
        OpsiAwalPanel.SetActive(false);
        AksiMelangkahPanel.SetActive(false);
        AksiTangkisPanel.SetActive(false);
        AksiSergapPanel.SetActive(false);
        if (KemenanganPanel != null) KemenanganPanel.SetActive(false);
    }
    
    public void UpdateMainDeckUI(int cardCount)
    {
        if (kartuDeckText == null) return;
        if (cardCount > 0)
        {
            kartuDeckText.gameObject.SetActive(true);
            kartuDeckText.text = "Sisa: " + cardCount;
        }
        else { kartuDeckText.text = "Deck Habis!"; }
    }
}