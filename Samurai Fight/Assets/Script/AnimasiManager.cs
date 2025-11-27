using UnityEngine;

public class AnimasiManager : MonoBehaviour
{
    public static AnimasiManager Instance;

    [Header("Animator References")]
    public Animator playerAnimator;
    public Animator aiAnimator;

    [Header("Parameter Names")]
    private string walkingBool = "isWalking";
    private string walkingBackwardBool = "isWalkingBackward";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

       public void SetWalking(bool isAI, bool isWalking)
    {
        Animator targetAnimator = isAI ? aiAnimator : playerAnimator;

        if (targetAnimator != null)
        {
            targetAnimator.SetBool(walkingBool, isWalking);
        }
        else
        {
            Debug.LogWarning($"Animator untuk {(isAI ? "AI" : "Player")} belum di-assign di AnimasiManager.");
        }
    }

    public void SetWalkingBackward(bool isAI, bool isWalking)
    {
        Animator targetAnimator = isAI ? aiAnimator : playerAnimator;
        if (targetAnimator != null)
        {
            targetAnimator.SetBool(walkingBackwardBool, isWalking);
        }
    }

    public void ResetAllAnimations()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isWalking", false);
            playerAnimator.SetBool("isWalkingBackward", false);
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        if (aiAnimator != null)
        {
            aiAnimator.SetBool("isWalking", false);
            aiAnimator.SetBool("isWalkingBackward", false);
            aiAnimator.Rebind();
            aiAnimator.Update(0f);
        }
    }

}