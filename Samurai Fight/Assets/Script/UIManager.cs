using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    // Bagian atas skrip tidak berubah...
    [Header("Setup Score")]
    public Sprite getScoreSprite;
    public Image[] scoreManusiaImages;
    public Image[] scoreAiImages;

    [Header("Card Display")]
    public GameObject cardPrefab;
    public Transform KartuManusiaPanel;

    [Header("Text System")]
    public TextMeshProUGUI nilaiSerangText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI KemenanganText;
    public TextMeshProUGUI sisaKartuDeckText;

    [Header("Panels")]
    public GameObject OpsiAwalPanel;
    public GameObject AksiMelangkahPanel;
    public GameObject AksiTangkisPanel;
    public GameObject AksiSergapPanel;
    public GameObject PerkuatSeranganPanel;
    public GameObject KemenanganPanel;

    [Header("Perpindahan Panel Kartu")]
    public Transform posisiDefault;
    public Transform posisiKanan;   // <-- DITUKAR, sebelumnya posisiTengah
    public Transform posisiTengah; // <-- DITUKAR, sebelumnya posisiKanan

    [Header("Panel Yang Disembunyikan")]
    public GameObject[] fokusMode;

    [Header("Action Buttons")]
    public Button MelangkahButton;
    public Button SerangButton;
    public Button MajuButton;
    public Button MundurButton;

    private Coroutine messageCoroutine;
    
    // Fungsi lainnya tetap sama sampai ShowHandPanelOnly
    // ...
    
    public void ShowHandPanelOnly(string context)
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);

        if (KartuManusiaPanel != null)
        {
            KartuManusiaPanel.gameObject.SetActive(true);

            // Logika di dalam sini juga ikut ditukar agar fungsinya tetap benar
            if (context == "attack" && posisiTengah != null) // <-- DITUKAR
            {
                KartuManusiaPanel.position = posisiTengah.position; // <-- DITUKAR
            }
            else if (context == "move" && posisiKanan != null) // <-- DITUKAR
            {
                KartuManusiaPanel.position = posisiKanan.position; // <-- DITUKAR
            }
        }
    }
    
    // Sisa skrip di bawah ini tidak ada perubahan...
    void Awake()
    {
        instance = this;
        if (nilaiSerangText != null) nilaiSerangText.gameObject.SetActive(false);
        if (systemText != null) systemText.gameObject.SetActive(false);
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
        KemenanganText.text = playerWon ? "ANDA MENANG!" : "ANDA KALAH!";
    }

    public void RestoreDefaultLayout()
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(true);
        if (KartuManusiaPanel != null && posisiDefault != null)
        {
            KartuManusiaPanel.position = posisiDefault.position;
        }
    }

    private void SetPersistentUIVisibility(bool isVisible)
    {
        foreach (var panel in fokusMode)
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
            if (KartuManusiaPanel != null && posisiDefault != null)
            {
                KartuManusiaPanel.position = posisiDefault.position;
            }
        }
    }

    public void UpdateScoreUI(Player player)
    {
        Image[] targetImages = player.isAI ? scoreAiImages : scoreManusiaImages;
        for (int i = 0; i < targetImages.Length; i++)
        {
            targetImages[i].sprite = (i < player.score) ? getScoreSprite : null;
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

    public void ShowPerkuatSeranganPanel(bool show)
    {
        HideAllPlayerPanels();
        SetPersistentUIVisibility(false);
        if (KartuManusiaPanel != null) KartuManusiaPanel.gameObject.SetActive(false);

        PerkuatSeranganPanel.SetActive(show);
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        systemText.text = message;
        systemText.gameObject.SetActive(true);

        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            systemText.gameObject.SetActive(false);
        }
    }

    public void HideMessage()
    {
        if (systemText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        systemText.gameObject.SetActive(false);
    }

    public void ShowAttackStrength(int strength)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"KEKUATAN SERANGAN: {strength}";
    }

    public void UpdateSelectedParryTotal(int total)
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(true);
        nilaiSerangText.text = $"TOTAL TANGKISAN: {total}";
    }

    public void HideAttackStrength()
    {
        if (nilaiSerangText == null) return;
        nilaiSerangText.gameObject.SetActive(false);
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
        if (PerkuatSeranganPanel != null) PerkuatSeranganPanel.SetActive(false);
        if (KemenanganPanel != null) KemenanganPanel.SetActive(false);
    }

    public void UpdateMainDeckUI(int cardCount)
    {
        if (sisaKartuDeckText == null) return;
        if (cardCount > 0)
        {
            sisaKartuDeckText.gameObject.SetActive(true);
            sisaKartuDeckText.text = "Sisa: " + cardCount;
        }
        else { sisaKartuDeckText.text = "Deck Habis!"; }
    }
}