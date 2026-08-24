using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundOnOff : MonoBehaviour
{
    public Toggle sfxToggle;
    public Toggle musicToggle;

    public void Start()
    {
        if (PlayerPrefs.HasKey("SFXOff") == true)
        {
            sfxToggle.isOn = false;
        }
        else if (PlayerPrefs.HasKey("SFXOff") == false)
        {
            sfxToggle.isOn = true;
        }

        if (PlayerPrefs.HasKey("MusicOff") == true)
        {
            musicToggle.isOn = false;
        }
        else if (PlayerPrefs.HasKey("MusicOff") == false)
        {
            musicToggle.isOn = true;
        }
    }

    public void MusicOnOff()
    {
        if (musicToggle.isOn == false)
        {
            PlayerPrefs.SetInt("MusicOff", 1);
        }
        else if (musicToggle.isOn == true)
        {
            PlayerPrefs.DeleteKey("MusicOff");
        }

        SoundFXManager.instance.PlayBGMusic();
    }

    public void SFXOnOff()
    {
        if (sfxToggle.isOn == false)
        {
            PlayerPrefs.SetInt("SFXOff", 1);
        }
        else if (sfxToggle.isOn == true)
        {
            PlayerPrefs.DeleteKey("SFXOff");
        }
    }
}
