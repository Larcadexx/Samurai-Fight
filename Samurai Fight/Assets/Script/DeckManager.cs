using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [SerializeField] 
    private List<Card> deck = new List<Card>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetupDeck()
    {
        CreateDeck();
        AcakDeck();
    }

    private void CreateDeck()
    {
        deck.Clear();
        for (int value = 1; value <= 5; value++)
        {
            for (int i = 0; i < 5; i++)
            {
                deck.Add(new Card(value));
            }
        }
    }

    public void AcakDeck()
    {
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = deck[k];
            deck[k] = deck[n];
            deck[n] = temp;
        }
    }

    public Card AcakCard()
    {
        if (deck.Count == 0) return null;

        Card drawnCard = deck[0];
        deck.RemoveAt(0);
        return drawnCard;
    }

    public int DeckCount()
    {
        return deck.Count;
    }

    public bool IsDeckEmpty()
    {
        return deck.Count == 0;
    }
}