using System.Collections;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [Header("Referensi Objek Visual")]
    public GameObject pionManusia;
    public GameObject pionAI;
    public Transform[] petakPapan;

    [Header("Pengaturan Animasi")]
    [Tooltip("Aktifkan agar script otomatis menghitung kecepatan animasi yang pas.")]
    public bool gunakanAutoSync = true;

    [Tooltip("Berapa petak yang dilewati dalam 1 siklus animasi (Kiri + Kanan)? Biasanya 2 petak.")]
    public float petakPerSiklusAnimasi = 2.0f;

    [Tooltip("Waktu (detik) yang dibutuhkan untuk pindah 1 petak. Semakin KECIL = Gerak Semakin CEPAT.")]
    public float durasiGerakPerKotak = 0.8f;

    [Tooltip("Geser X/Z untuk ke tengah, Geser Y untuk tinggi (agar kaki napak).")]
    public Vector3 offsetPion = new Vector3(0, 0, 0);

    private Animator pionManusiaAnimator;
    private Animator pionAIAnimator;

    public void Initialize()
    {
        if (pionManusia != null) pionManusiaAnimator = pionManusia.GetComponent<Animator>();
        if (pionAI != null) pionAIAnimator = pionAI.GetComponent<Animator>();

        ResetAnimationState();
    }

    public void ResetAnimationState()
    {
        if (pionManusiaAnimator != null) pionManusiaAnimator.SetBool("isWalking", false);
        if (pionAIAnimator != null) pionAIAnimator.SetBool("isWalking", false);
    }

    public void MovePionVisual(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        if (targetPosition > 0 && targetPosition <= petakPapan.Length)
        {
            Transform targetPetak = petakPapan[targetPosition - 1];
            pawn.transform.position = targetPetak.position + offsetPion;
        }
    }

    public IEnumerator MovePionStepByStep(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia;
        Animator currentAnimator = player.isAI ? pionAIAnimator : pionManusiaAnimator;

        int currentPos = player.position;
        int step = (targetPosition > currentPos) ? 1 : -1;

        if (currentAnimator != null)
        {
            currentAnimator.SetBool("isWalking", true);
        }

        yield return null; 

        if (currentAnimator != null && gunakanAutoSync)
        {
            currentAnimator.Update(0); 

            AnimatorStateInfo stateInfo = currentAnimator.GetCurrentAnimatorStateInfo(0);
            
            if (currentAnimator.IsInTransition(0))
            {
                stateInfo = currentAnimator.GetNextAnimatorStateInfo(0);
            }

            float originalClipDuration = stateInfo.length;
            
            if (originalClipDuration <= 0 || float.IsInfinity(originalClipDuration)) 
            {
                originalClipDuration = 1.2f; 
            }

            float targetDurationForCycle = durasiGerakPerKotak * petakPerSiklusAnimasi;

            if (targetDurationForCycle > 0)
            {
                currentAnimator.speed = originalClipDuration / targetDurationForCycle;
            }
        }
        else if (currentAnimator != null)
        {
            currentAnimator.speed = 1f; 
        }

        float moveDuration = durasiGerakPerKotak;
        float speed = 1.0f / moveDuration;

        for (int pos = currentPos; pos != targetPosition; pos += step)
        {
            int nextPos = pos + step;

            if (nextPos > 0 && nextPos <= petakPapan.Length)
            {
                Vector3 startPos = petakPapan[pos - 1].position + offsetPion;
                Vector3 endPos = petakPapan[nextPos - 1].position + offsetPion;
                float t = 0f;

                while (t < 1f)
                {
                    t += Time.deltaTime * speed;
                    
                    float smoothT = t * t * (3f - 2f * t); 
                    
                    pawn.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                    yield return null;
                }
                pawn.transform.position = endPos;
            }
        }

        player.position = targetPosition;

        if (currentAnimator != null)
        {
            currentAnimator.SetBool("isWalking", false);
            currentAnimator.speed = 1f;
        }
    }
}