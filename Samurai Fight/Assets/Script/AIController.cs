using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    private GameManager gameManager;
    private UIManager uiManager;
    private Player aiPlayer;
    private Player manusiaPlayer;

    public void Initialize(GameManager gm, UIManager um)
    {
        gameManager = gm;
        uiManager = um;
    }

    public IEnumerator ExecuteTurn(Player ai, Player player)
    {
        this.aiPlayer = ai;
        this.manusiaPlayer = player;

        if (uiManager.panelInfo != null) uiManager.panelInfo.SetActive(true);
        uiManager.ShowMessage("Giliran AI...", 0f);
        
        yield return new WaitForSeconds(2.0f);

        if (aiPlayer.hand.Count == 0) 
        { 
            gameManager.EndTurn(); 
            yield break; 
        }

        int distance = aiPlayer.position - manusiaPlayer.position;
        bool canSerang = aiPlayer.hand.Any(kartu => kartu.value == distance);
        bool canMaju = aiPlayer.hand.Any(kartu => aiPlayer.position - kartu.value > manusiaPlayer.position);
        bool canMundur = aiPlayer.hand.Any(kartu => aiPlayer.position + kartu.value <= 23);

        if (canSerang)
        {
            Card kartuSerang = aiPlayer.hand.First(kartu => kartu.value == distance);
            uiManager.ShowMessage($"AI menyerang dengan kekuatan {kartuSerang.value}.", 2.5f);
            yield return new WaitForSeconds(2.0f);
            aiPlayer.hand.Remove(kartuSerang);
            
            gameManager.InitiateSerangan(aiPlayer, manusiaPlayer, kartuSerang.value, isCounter: false);
        }
        else if (canMaju)
        {
            var validKartu = aiPlayer.hand.Where(kartu => aiPlayer.position - kartu.value > manusiaPlayer.position).ToList();
            Card selectedKartu = validKartu[Random.Range(0, validKartu.Count)];
            
            int targetPosition = aiPlayer.position - selectedKartu.value;
            uiManager.ShowMessage("AI melangkah maju.", 1.5f); 
            
            yield return gameManager.StartCoroutine(gameManager.MovePionPerLangkah(aiPlayer, targetPosition));
            
            aiPlayer.hand.Remove(selectedKartu);
            StartCoroutine(CheckAISergapCoroutine());
        }
        else if (canMundur)
        {
            var validKartu = aiPlayer.hand.Where(kartu => aiPlayer.position + kartu.value <= 23).ToList();
            Card selectedKartu = validKartu[Random.Range(0, validKartu.Count)];
            
            int targetPosition = aiPlayer.position + selectedKartu.value;
            uiManager.ShowMessage("AI melangkah mundur.", 1.5f);
            
            yield return gameManager.StartCoroutine(gameManager.MovePionPerLangkah(aiPlayer, targetPosition));
            
            aiPlayer.hand.Remove(selectedKartu);
            StartCoroutine(CheckAISergapCoroutine());
        }
        else
        {
            StartCoroutine(gameManager.DelayTieBreaker(GameManager.TieBreakerReason.PlayerTrapped));
        }
    }

    public void HandleAIAttacked(Player ai, Player player, int attackValue, bool isCounter)
    {
        this.aiPlayer = ai;
        this.manusiaPlayer = player;
        StartCoroutine(DecideAITangkisCoroutine(attackValue, isCounter));
    }

    private IEnumerator DecideAITangkisCoroutine(int attackValue, bool isThisSerangCounter)
    {
        yield return new WaitForSeconds(2.5f);
        List<Card> kombinasiTangkis = FindKombinasiTangkis(attackValue, aiPlayer.hand, !isThisSerangCounter);

        if (kombinasiTangkis != null)
        {
            uiManager.PlayCutscene(uiManager.clipAIBerhasilTangkis, () => 
            {
                gameManager.StartCoroutine(ProcessSuccessfulAITangkis(kombinasiTangkis, isThisSerangCounter));
            });
        }
        else
        {
            uiManager.PlayCutscene(uiManager.clipAIGagalTangkis, () => 
            {
                gameManager.StartCoroutine(ProcessFailedAITangkis());
            });
        }
    }

    private IEnumerator ProcessSuccessfulAITangkis(List<Card> kombinasiTangkis, bool isThisSerangCounter)
    {
        uiManager.ShowMessage($"AI berhasil menangkis serangan Anda.", 3.0f);
        yield return new WaitForSeconds(3.0f);
        foreach (var kartu in kombinasiTangkis) aiPlayer.hand.Remove(kartu);

        if (isThisSerangCounter)
        {
            gameManager.EndTurn();
        }
        else
        {
            StartCoroutine(ExecuteAISerangBalikCoroutine());
        }
    }

    private IEnumerator ProcessFailedAITangkis()
    {
        uiManager.ShowMessage($"AI gagal menangkis serangan Anda.", 2.5f);
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(gameManager.DelayRondeEnd(manusiaPlayer)); 
    }

    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (aiPlayer.hand.Count == 0)
        {
            gameManager.EndTurn();
            yield break;
        } 
        Card kartuCounter = aiPlayer.hand.OrderBy(kartu => kartu.value).First(); 
        uiManager.ShowMessage($"AI melakukan Serang Balik dengan kekuatan {kartuCounter.value}!", 2.5f);
        yield return new WaitForSeconds(2.5f);
        aiPlayer.hand.Remove(kartuCounter);
        gameManager.InitiateSerangan(aiPlayer, manusiaPlayer, kartuCounter.value, true);
    }

    private IEnumerator CheckAISergapCoroutine()
    {
        yield return new WaitForSeconds(2.5f);

        int newDistance = aiPlayer.position - manusiaPlayer.position;
        bool canSergap = aiPlayer.hand.Any(kartu => kartu.value == newDistance);

        if (canSergap && aiPlayer.hand.Count > 0)
        {
            Card kartuSergap = aiPlayer.hand.First(kartu => kartu.value == newDistance);
            
            uiManager.ShowMessage($"AI melakukan Sergap dengan kekuatan {kartuSergap.value}!", 2.5f);
            yield return new WaitForSeconds(2.5f);

            aiPlayer.hand.Remove(kartuSergap);
            gameManager.InitiateSerangan(aiPlayer, manusiaPlayer, kartuSergap.value, false, false); 
        }
        else
        {
            gameManager.EndTurn();
        }
    }

    private List<Card> FindKombinasiTangkis(int target, List<Card> handKartu, bool mustLeaveKartu)
    {
        List<Card> FindSubsetSum(int currentTarget, List<Card> currentKartu, List<Card> currentKombinasi)
        {
            if (currentTarget == 0)
            {
                if (mustLeaveKartu && handKartu.Count - currentKombinasi.Count < 1) return null;
                return currentKombinasi;
            }
            if (currentTarget < 0 || currentKartu.Count == 0) return null;
            Card head = currentKartu[0];
            List<Card> tail = currentKartu.GetRange(1, currentKartu.Count - 1);
            var withHead = new List<Card>(currentKombinasi) { head };
            var resultWith = FindSubsetSum(currentTarget - head.value, tail, withHead);
            if (resultWith != null) return resultWith;
            var resultWithout = FindSubsetSum(currentTarget, tail, currentKombinasi);
            return resultWithout;
        }
        return FindSubsetSum(target, new List<Card>(handKartu), new List<Card>());
    }
}