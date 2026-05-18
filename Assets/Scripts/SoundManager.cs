using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private List<Sound> sounds = new();

    private Dictionary<SoundType, AudioClip> soundDictionary;

    [SerializeField] private AudioSource audioSourceSFX;
    [SerializeField] private AudioSource audioSourceBG;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        SoundSetup();
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void BeforeSceneLoad()
    {
        PlayerPrefs.SetFloat("Music", audioSourceBG.volume);
        PlayerPrefs.SetFloat("SFX", audioSourceSFX.volume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        audioSourceBG.volume = PlayerPrefs.GetFloat("Music");
        audioSourceSFX.volume = PlayerPrefs.GetFloat("SFX");
    }

    public void MusicSlider(Slider slider)
    {
        audioSourceBG.volume = slider.value;
    }

    public void SFXSlider(Slider slider)
    {
        audioSourceSFX.volume = slider.value;
    }

    private void SoundSetup()
    {
        soundDictionary = new Dictionary<SoundType, AudioClip>();

        foreach (Sound sound in sounds)
        {
            if (!soundDictionary.ContainsKey(sound.type))
            {
                soundDictionary.Add(sound.type, sound.clip);
            }
            else
            {
                Debug.LogWarning("Duplicate sound type");
            }
        }
    }

    public void PlayMusic(SoundType type)
    {
        if (soundDictionary.TryGetValue(type, out AudioClip clip))
        {
            audioSourceBG.clip = clip;
            audioSourceBG.Play();
        }
        else
        {
            Debug.LogWarning("Music not found");
        }
    }

    public void PlaySound(SoundType type)
    {
        if (soundDictionary.TryGetValue(type, out AudioClip clip))
        {
            audioSourceSFX.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("Sound not found");
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("Music", audioSourceBG.volume);
        PlayerPrefs.SetFloat("SFX", audioSourceSFX.volume);
    }
}

[System.Serializable]
public class Sound
{
    public AudioClip clip;
    public SoundType type;
}

public enum SoundType
{
    MainMenuMusic,
    BattleMusic,

    PlayerAttack,
    Block,
    UI,
    PlayerDead,
    EnemyDead,
    EnemyAttack,
    EnemyInjured
}