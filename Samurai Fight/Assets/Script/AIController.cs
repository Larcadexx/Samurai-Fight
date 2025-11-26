using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    private GameManager gameManager;
    private UIManager uiManager;
    private Player aiPlayer;
    private Player humanPlayer;

    public void Initialize(GameManager gm, UIManager um)
    {
        gameManager = gm;
        uiManager = um;
    }

    public IEnumerator ExecuteTurn(Player ai, Player player)
{
    this.aiPlayer = ai;
    this.humanPlayer = player;

    uiManager.ShowInfo_AITurn();
    yield return new WaitForSeconds(2.0f);

    if (aiPlayer.hand.Count == 0) 
    { 
        gameManager.EndTurn(); 
        yield break; 
    }

    int distance = aiPlayer.position - humanPlayer.position;
    bool canAttack = aiPlayer.hand.Any(card => card.value == distance);
    bool canMoveForward = aiPlayer.hand.Any(card => aiPlayer.position - card.value > humanPlayer.position);
    bool canMoveBackward = aiPlayer.hand.Any(card => aiPlayer.position + card.value <= 23);

    if (canAttack)
    {
        Card attackCard = aiPlayer.hand.First(card => card.value == distance);
        uiManager.ShowMessage($"AI menyerang dengan kekuatan {attackCard.value}.", 2.5f);
        yield return new WaitForSeconds(2.0f);
        aiPlayer.hand.Remove(attackCard);
        gameManager.InisiasiSerangan(aiPlayer, humanPlayer, attackCard.value, isCounter: false);
    }
    else if (canMoveForward)
    {
        var validCards = aiPlayer.hand.Where(card => aiPlayer.position - card.value > humanPlayer.position).ToList();
        Card chosenCard = validCards[Random.Range(0, validCards.Count)];
        
        // --- PERUBAHAN MULAI DI SINI (Melangkah Maju) ---
        
        // 1. Hitung target posisi (JANGAN update aiPlayer.position di sini, biarkan animasi yang update)
        int targetPos = aiPlayer.position - chosenCard.value;

        uiManager.ShowMessage("AI melangkah maju.", 1.5f); // Waktu pesan dikurangi sedikit agar tidak terlalu lama
        
        // 2. Jalankan animasi berjalan (tunggu sampai selesai)
        yield return gameManager.StartCoroutine(gameManager.MovePionStepByStep(aiPlayer, targetPos));
        
        // 3. Hapus kartu
        aiPlayer.hand.Remove(chosenCard);
        StartCoroutine(CheckForAISergapCoroutine());

        // --- PERUBAHAN SELESAI ---
    }
    else if (canMoveBackward)
    {
        var validCards = aiPlayer.hand.Where(card => aiPlayer.position + card.value <= 23).ToList();
        Card chosenCard = validCards[Random.Range(0, validCards.Count)];
        
        // --- PERUBAHAN MULAI DI SINI (Melangkah Mundur) ---
        
        // 1. Hitung target posisi
        int targetPos = aiPlayer.position + chosenCard.value;

        uiManager.ShowMessage("AI melangkah mundur.", 1.5f);
        
        // 2. Jalankan animasi berjalan
        yield return gameManager.StartCoroutine(gameManager.MovePionStepByStep(aiPlayer, targetPos));
        
        // 3. Hapus kartu
        aiPlayer.hand.Remove(chosenCard);
        StartCoroutine(CheckForAISergapCoroutine());

        // --- PERUBAHAN SELESAI ---
    }
    else
    {
        StartCoroutine(gameManager.TieBreakerDelay(GameManager.TieBreakerReason.PlayerCornered));
    }
}

    public void HandleAIAttacked(Player ai, Player player, int attackValue, bool isCounter)
    {
        this.aiPlayer = ai;
        this.humanPlayer = player;
        StartCoroutine(DecideAITangkisCoroutine(attackValue, isCounter));
    }

    private IEnumerator DecideAITangkisCoroutine(int attackValue, bool isCurrentAttackACounter)
    {
        yield return new WaitForSeconds(2.5f);
        List<Card> parryCombination = KombinasiTangkisan(attackValue, aiPlayer.hand, !isCurrentAttackACounter);

        if (parryCombination != null)
        {
            uiManager.ShowMessage($"AI berhasil menangkis serangan Anda.", 3.0f);
            yield return new WaitForSeconds(3.0f);
            foreach (var card in parryCombination) aiPlayer.hand.Remove(card);

            if (isCurrentAttackACounter)
            {
                gameManager.EndTurn();
            }
            else
            {
                StartCoroutine(ExecuteAISerangBalikCoroutine());
            }
        }
        else
        {
            uiManager.ShowMessage($"AI gagal menangkis serangan Anda.", 2.5f);
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(gameManager.RoundOverDelay(humanPlayer));
        }
    }

    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (aiPlayer.hand.Count == 0)
        {
            gameManager.EndTurn();
            yield break;
        } 
        Card counterCard = aiPlayer.hand.OrderBy(card => card.value).First(); 
        uiManager.ShowMessage($"AI melakukan Serang Balik dengan kekuatan {counterCard.value}!", 2.5f);
        yield return new WaitForSeconds(2.5f);
        aiPlayer.hand.Remove(counterCard);
        gameManager.InisiasiSerangan(aiPlayer, humanPlayer, counterCard.value, true);
    }

    private IEnumerator CheckForAISergapCoroutine()
    {
        yield return new WaitForSeconds(2.5f);

        int newDistance = aiPlayer.position - humanPlayer.position;
        bool canSergap = aiPlayer.hand.Any(card => card.value == newDistance);

        if (canSergap && aiPlayer.hand.Count > 0)
        {
            Card sergapCard = aiPlayer.hand.First(card => card.value == newDistance);
            
            uiManager.ShowMessage($"AI melakukan Sergap dengan kekuatan {sergapCard.value}!", 2.5f);
            yield return new WaitForSeconds(2.5f);

            aiPlayer.hand.Remove(sergapCard);
            gameManager.InisiasiSerangan(aiPlayer, humanPlayer, sergapCard.value, false, false); 
        }
        else
        {
            gameManager.EndTurn();
        }
    }

    private List<Card> KombinasiTangkisan(int target, List<Card> hand, bool mustHaveCardLeft)
    {
        List<Card> FindSubsetSum(int currentTarget, List<Card> currentHand, List<Card> currentCombination)
        {
            if (currentTarget == 0)
            {
                if (mustHaveCardLeft && hand.Count - currentCombination.Count < 1) return null;
                return currentCombination;
            }
            if (currentTarget < 0 || currentHand.Count == 0) return null;
            Card head = currentHand[0];
            List<Card> tail = currentHand.GetRange(1, currentHand.Count - 1);
            var withHead = new List<Card>(currentCombination) { head };
            var resultWith = FindSubsetSum(currentTarget - head.value, tail, withHead);
            if (resultWith != null) return resultWith;
            var resultWithout = FindSubsetSum(currentTarget, tail, currentCombination);
            return resultWithout;
        }
        return FindSubsetSum(target, new List<Card>(hand), new List<Card>());
    }
}