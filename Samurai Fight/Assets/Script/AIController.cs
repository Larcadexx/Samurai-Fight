using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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

        if (uiManager.InfoPanel != null) uiManager.InfoPanel.SetActive(true);
        uiManager.ShowMessage("Giliran AI...", 0f);
        
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
            
            int targetPos = aiPlayer.position - chosenCard.value;
            uiManager.ShowMessage("AI melangkah maju.", 1.5f); 
            
            yield return gameManager.StartCoroutine(gameManager.MovePionStepByStep(aiPlayer, targetPos));
            
            aiPlayer.hand.Remove(chosenCard);
            StartCoroutine(CheckForAISergapCoroutine());
        }
        else if (canMoveBackward)
        {
            var validCards = aiPlayer.hand.Where(card => aiPlayer.position + card.value <= 23).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)];
            
            int targetPos = aiPlayer.position + chosenCard.value;
            uiManager.ShowMessage("AI melangkah mundur.", 1.5f);
            
            yield return gameManager.StartCoroutine(gameManager.MovePionStepByStep(aiPlayer, targetPos));
            
            aiPlayer.hand.Remove(chosenCard);
            StartCoroutine(CheckForAISergapCoroutine());
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
            uiManager.PlayCutscene(uiManager.clipAITangkisSukses, () => 
            {
                gameManager.StartCoroutine(ProcessAISuccessParry(parryCombination, isCurrentAttackACounter));
            });
        }
        else
        {
            uiManager.PlayCutscene(uiManager.clipAITangkisGagal, () => 
            {
                gameManager.StartCoroutine(ProcessAIFailedParry());
            });
        }
    }

    private IEnumerator ProcessAISuccessParry(List<Card> parryCombination, bool isCurrentAttackACounter)
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

    private IEnumerator ProcessAIFailedParry()
    {
        uiManager.ShowMessage($"AI gagal menangkis serangan Anda.", 2.5f);
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(gameManager.RoundOverDelay(humanPlayer)); 
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