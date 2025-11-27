using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [SerializeField] 
    private List<Card> dek = new List<Card>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetupDek()
    {
        CreateDek();
        ShuffleDek();
    }

    private void CreateDek()
    {
        dek.Clear();
        for (int value = 1; value <= 5; value++)
        {
            for (int i = 0; i < 5; i++)
            {
                dek.Add(new Card(value));
            }
        }
    }

    public void ShuffleDek()
    {
        for (int i = 0; i < dek.Count; i++)
        {
            Card temp = dek[i];
            int randomIndex = Random.Range(i, dek.Count);
            dek[i] = dek[randomIndex];
            dek[randomIndex] = temp;
        }
    }

    public Card DrawDek()
    {
        if (dek.Count == 0) return null;

        Card drawnCard = dek[0];
        dek.RemoveAt(0);
        return drawnCard;
    }

    public int GetSisaDek()
    {
        return dek.Count;
    }

    public bool IsDekEmpty()
    {
        return dek.Count == 0;
    }
}