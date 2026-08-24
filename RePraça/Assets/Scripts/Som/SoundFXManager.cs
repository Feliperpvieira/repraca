using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    //original code from: https://youtu.be/DU7cgVsU2rM

    //How to call this script:
    //Create a
    //public AudioClip nameOfAudioFX;

    //Call the audio on the script with a
    //SoundFXManager.instance.PlaySoundFXClip(nameOfAudioFX, transform, 1f);
    //Done!

    public static SoundFXManager instance; //makes it a singleton // !!! only have 1 of these per scene
    [SerializeField] private AudioSource soundFXObject;

    public AudioSource backgroundMusic;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void Start()
    {
        PlayBGMusic(); //calls the function on start of a scene to play or pause bg music
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        //Debug.Log("SFXOff:  " + PlayerPrefs.HasKey("SFXOff"));
        if (PlayerPrefs.HasKey("SFXOff") == false)
        {
            //instantiate gameobject
            AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

            //assign the audioclip
            audioSource.clip = audioClip;

            //assign volume
            audioSource.volume = volume;

            //play sound
            audioSource.Play();

            //get length of sound fx clip
            float clipLength = audioSource.clip.length;

            //destroy the clip after it is done playing
            Destroy(audioSource.gameObject, clipLength);
        }

    }

    public void PlayBGMusic()
    {
        if (PlayerPrefs.HasKey("MusicOff") == false)
        {
            backgroundMusic.Play();
        }
        else if (PlayerPrefs.HasKey("MusicOff") == true)
        {
            backgroundMusic.Pause();
        }
    }
}
