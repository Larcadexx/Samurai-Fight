using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransisiScene : MonoBehaviour
{
    public static TransisiScene Instance;

    [Header("Komponen UI")]
    public CanvasGroup fadeCanvasGroup;
    public GameObject fadeCanvasObject;

    [Header("Pengaturan Waktu")]
    public float durasiFade = 1.0f; 
    public float durasiTunggu = 1.0f; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void PindahKeScene(string namaScene)
    {
        StartCoroutine(ProsesTransisi(namaScene));
    }

    private IEnumerator ProsesTransisi(string namaScene)
    {
        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene(namaScene);

        yield return new WaitForSeconds(durasiTunggu);

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        fadeCanvasObject.SetActive(true);
        float timer = 0f;

        while (timer < durasiFade)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / durasiFade);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < durasiFade)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / durasiFade));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasObject.SetActive(false);
    }
}