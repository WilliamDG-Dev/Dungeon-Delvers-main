using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private List<Sound> sounds = new();

    private Dictionary<SoundType, AudioClip> soundDictionary;

    [SerializeField] private AudioSource audioSourceSFX;
    [SerializeField] private AudioSource audioSourceBG;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

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
            return;
        }

        SoundSetup();
        SliderSetup();
    }

    private void SliderSetup()
    {
        musicSlider.onValueChanged.AddListener(MusicSlider);
        sfxSlider.onValueChanged.AddListener(SFXSlider);
    }
    private void MusicSlider(float value)
    {
        audioSourceBG.volume = musicSlider.value;
    }
    private void SFXSlider(float value)
    {
        audioSourceSFX.volume = sfxSlider.value;
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