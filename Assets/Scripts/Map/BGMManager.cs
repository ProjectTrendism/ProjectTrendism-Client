using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("씬별 BGM")]
    public AudioClip titleBGM;
    public AudioClip exploreBGM;
    public AudioClip craftBGM;
    public AudioClip sellBGM;
    public AudioClip defaultBGM;

    [Header("볼륨 설정")]
    [Range(0f, 1f)]
    public float masterVolume = 0.5f;

    [Header("페이드 설정")]
    public float fadeTime = 1.0f;

    private AudioClip currentClip;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSource();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = masterVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    public void PlayBGMForScene(string sceneName)
    {
        AudioClip targetClip = GetBGMBySceneName(sceneName);

        if (targetClip == null)
        {
            Debug.LogWarning("[BGMManager] 재생할 BGM이 없습니다. Scene: " + sceneName);
            return;
        }

        if (currentClip == targetClip)
        {
            return;
        }

        ChangeBGM(targetClip);
    }

    private AudioClip GetBGMBySceneName(string sceneName)
    {
        if (sceneName.Contains("Title"))
            return titleBGM != null ? titleBGM : defaultBGM;

        if (sceneName.Contains("Explore") || sceneName.Contains("village") || sceneName.Contains("villagee"))
            return exploreBGM != null ? exploreBGM : defaultBGM;

        if (sceneName.Contains("Craft"))
            return craftBGM != null ? craftBGM : defaultBGM;

        if (sceneName.Contains("Sell") || sceneName.Contains("Market"))
            return sellBGM != null ? sellBGM : defaultBGM;

        return defaultBGM;
    }

    public void ChangeBGM(AudioClip newClip)
    {
        if (newClip == null)
            return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(ChangeBGMRoutine(newClip));
    }

    private IEnumerator ChangeBGMRoutine(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        currentClip = newClip;
        audioSource.Play();

        while (audioSource.volume < masterVolume)
        {
            audioSource.volume += masterVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.volume = masterVolume;
        fadeRoutine = null;
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
    }

    public void StopBGM()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(StopBGMRoutine());
    }

    private IEnumerator StopBGMRoutine()
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = null;
        currentClip = null;
        audioSource.volume = masterVolume;
        fadeRoutine = null;
    }
}