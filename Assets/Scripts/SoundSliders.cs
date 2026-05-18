using UnityEngine;
using UnityEngine.UI;

public class SoundSliders : MonoBehaviour
{
    [SerializeField] private Type soundType;
    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();

        SetSliders();
    }

    private void SetSliders()
    {
        if (soundType == Type.Music)
        {
            slider.value = PlayerPrefs.GetFloat("Music");
        }
        else if (soundType == Type.SFX)
        {
            slider.value = PlayerPrefs.GetFloat("SFX");
        }
    }

    public void SetValue()
    {
        if (soundType == Type.Music)
        {
            SoundManager.Instance.MusicSlider(slider);
        }
        else if (soundType == Type.SFX)
        {
            SoundManager.Instance.SFXSlider(slider);
        }
    }

}

public enum Type
{
    Music,
    SFX
}
