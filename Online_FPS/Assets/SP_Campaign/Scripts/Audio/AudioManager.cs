using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;


public class TrackInfo
{
    public string           name = string.Empty;
    public AudioMixerGroup  group = null;
    public IEnumerator      trackFader = null;

}

public class AudioManager : MonoBehaviour
{
    //Singleton
    private static AudioManager instance = null;
    public static AudioManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
            }

            return instance;
        }
    }

    //Inspector Assigned
    [SerializeField] AudioMixer mixer = null;

    //Private
    Dictionary<string, TrackInfo> tracks = new Dictionary<string, TrackInfo>();


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (mixer == null)
            return;

        //Fetch all the groups in the mixer - mixer tracks 
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(string.Empty);
        foreach (AudioMixerGroup group in groups)
        {
            //Create track info
            TrackInfo trackInfo = new TrackInfo();
            trackInfo.name = group.name;
            trackInfo.group = group;
            trackInfo.trackFader = null;
           
            tracks[group.name] = trackInfo; //Add track info to the dictionary 
        }
    }


    public float GetTrackVolume(string _track)
    {
        if(tracks.TryGetValue(_track, out TrackInfo trackInfo))
        {
            mixer.GetFloat(_track, out float volume);

            return volume;
        }

        return float.MinValue;
    }

    public AudioMixerGroup GetAudioGroupFromTrackName(string _name)
    {
        if (tracks.TryGetValue(_name, out TrackInfo trackInfo))
        {
            return trackInfo.group;
        }

        return null;
    }

    public void SetTrackVolume(string _track, float _volume, float _fadeTime = 0.0f)
    {
        if (mixer == null)
            return;
        
        if(tracks.TryGetValue(_track, out TrackInfo trackInfo))
        {
            //Stop any coroutine that might be in the middle of fading this track
            if (trackInfo.trackFader != null)
            {
                StopCoroutine(trackInfo.trackFader);
            }

            if(_fadeTime  == 0.0f)
            {
                mixer.SetFloat(_track, _volume);
            }
            else
            {
                trackInfo.trackFader = SetTrackVolumeInternal(_track, _volume, _fadeTime);
                StartCoroutine(trackInfo.trackFader);
            }
        }
    }

    protected IEnumerator SetTrackVolumeInternal(string _track, float _volume, float _fadeTime)
    {
        float startVolume = 0.0f;
        float timer = 0.0f;

        mixer.GetFloat(_track, out startVolume); //Get current volume

        while(timer < _fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            mixer.SetFloat(_track, Mathf.Lerp(startVolume, _volume, timer / _fadeTime));
            yield return null;
        }

        mixer.SetFloat(_track, _volume);
    }
}
