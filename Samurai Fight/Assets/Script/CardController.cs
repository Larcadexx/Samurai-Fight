using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Script ini mengatur tampilan kartu dan interaksinya di UI
public class CardController : MonoBehaviour
{
    private Card cardData;                     // Data kartu yang ditampilkan
    public TextMeshProUGUI valueCardText;      // Teks nilai kartu
    public Image background;                   // Background kartu
    private bool isSelected = false;           // Status apakah kartu sedang dipilih

    // Dipanggil saat kartu diinisialisasi (set nilai dan teks)
    public void Initialize(Card data)
    {
        cardData = data;
        valueCardText.text = data.value.ToString();
    }

    // Dipanggil saat pemain mengklik kartu
    public void OnClick()
    {
        if (cardData == null) return;

        // Kirim data kartu ke GameManager untuk diproses
        GameManager.instance.OnCardInHandClicked(cardData, this);
    }

    // Mengubah tampilan kartu saat dipilih / tidak
    public void ToggleSelection(bool select)
    {
        isSelected = select;
        background.color = isSelected ? Color.yellow : Color.white;
    }
}
