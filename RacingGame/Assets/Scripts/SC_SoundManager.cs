using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SC_SoundManager : MonoBehaviour
{
    public static SC_SoundManager instance { get; private set; }
    public AudioSource main_audiosource = null;

    public AudioMixerGroup main_mixer = null;

    public int max_sounds = 100;

    public List<AudioSource> list_audiosources;
    private int indx_audio = 0;

    private void Awake()
    {
        instance = this;

        // Init the audiosources
        list_audiosources = new List<AudioSource>();

        GameObject sounds_parent = new GameObject("Sounds parent");

        for (int i = 0; i < max_sounds; i++)
        {
            GameObject ob_audio = new GameObject($"AudioSource {i}");
            ob_audio.transform.parent = sounds_parent.transform;

            // Configure the audio source
            AudioSource audio_as = ob_audio.AddComponent<AudioSource>();
            audio_as.rolloffMode = AudioRolloffMode.Linear;
            audio_as.maxDistance = 50;
            audio_as.spatialBlend = 5;
            audio_as.outputAudioMixerGroup = main_mixer;

            list_audiosources.Add(audio_as);
        }
    }


    public void PlaySound(AudioClip clip)
    {
        main_audiosource.clip = clip;
        main_audiosource.Play();
    }

    public GameObject InstantiateSound(AudioClip clip, Vector3 position, bool loop = false, float volume = 1, float pitch = 1)
    {
        if (clip == null) return null;

        AudioSource audio_as = list_audiosources[indx_audio];
        audio_as.transform.position = position;
        audio_as.clip = clip;
        audio_as.loop = loop;
        audio_as.volume = volume;
        audio_as.pitch = pitch;
        audio_as.Play();

        indx_audio++;
        if (indx_audio >= list_audiosources.Count)
            indx_audio = 0;

        return audio_as.gameObject;
    }
    public AudioSource InstantiateSoundAlone(AudioClip clip, Vector3 position, bool loop = false, float volume = 1, float pitch = 1)
    {
        if (clip == null) return null;

        GameObject ob_audio = new GameObject($"AudioSource");

        // Configure the audio source
        AudioSource audio_as = ob_audio.AddComponent<AudioSource>();
        audio_as.playOnAwake = false;
        audio_as.clip = clip;
        audio_as.rolloffMode = AudioRolloffMode.Linear;
        audio_as.maxDistance = 50;
        audio_as.spatialBlend = 5;
        audio_as.outputAudioMixerGroup = main_mixer;


        return audio_as;
    }
}
