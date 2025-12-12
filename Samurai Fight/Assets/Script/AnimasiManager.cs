using UnityEngine;

public class AnimasiManager : MonoBehaviour
{
    public static AnimasiManager Instance;

    [Header("Animator References")]
    public Animator playerAnimator;
    public Animator aiAnimator;
    private int walkingHash;
    private int walkingBackwardHash;

    private const string PARAM_WALKING = "isWalking";
    private const string PARAM_WALKING_BACKWARD = "isWalkingBackward";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeAnimatorHashes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAnimatorHashes()
    {
        walkingHash = Animator.StringToHash(PARAM_WALKING);
        walkingBackwardHash = Animator.StringToHash(PARAM_WALKING_BACKWARD);
    }


    public void SetWalking(bool isAI, bool isWalking)
    {
        SetBoolToAnimator(isAI, walkingHash, isWalking);
    }

    public void SetWalkingBackward(bool isAI, bool isWalking)
    {
        SetBoolToAnimator(isAI, walkingBackwardHash, isWalking);
    }

    public void ResetAllAnimations()
    {
        ResetSingleAnimator(playerAnimator);
        ResetSingleAnimator(aiAnimator);
    }


    private void SetBoolToAnimator(bool isAI, int paramHash, bool value)
    {
        Animator target = isAI ? aiAnimator : playerAnimator;

        if (target != null)
        {
            target.SetBool(paramHash, value);
        }
        else
        {
            Debug.LogWarning($"Animator {(isAI ? "AI" : "Player")} bernilai null.");
        }
    }

    private void ResetSingleAnimator(Animator anim)
    {
        if (anim != null)
        {
            anim.SetBool(walkingHash, false);
            anim.SetBool(walkingBackwardHash, false);
            
            anim.Rebind(); 
            anim.Update(0f);
        }
    }
}