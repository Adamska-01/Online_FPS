using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine;


// -----------------------------------------------------------------------------
// CLASS	:	TrackInfo
// DESC		:	Wraps an AudioMixerGroup in Unity's AudioMixer. Contains the 
//              name of the group (which is also its exposed volume parameter), 
//              the group itself, and an IEnumerator for doing track fades over
//              time.
// -----------------------------------------------------------------------------
public class TrackInfo
{
    public string           name = string.Empty;
    public AudioMixerGroup  group = null;
    public IEnumerator      trackFader = null;
}


// -----------------------------------------------------------------------------
// CLASS	:	AudioPoolItem
// DESC		:	Describes an audio entity in the pooling system
// -----------------------------------------------------------------------------
public class AudioPoolItem
{
    public GameObject   gameObj      = null;
    public Transform    transf       = null;
    public AudioSource  audioSrc     = null;
    public float        unImportance = float.MaxValue;
    public bool         isPlaying    = false;
    public IEnumerator  coroutine    = null;
    public ulong        ID           = 0;
}


// -----------------------------------------------------------------------------
// CLASS	:	AudioManager
// DESC		:	Provides pooled one-shot functionality with priority system and
//              also wraos the Unity Audio Mixer to make easier manipulation of
//              audio group volumes
// -----------------------------------------------------------------------------
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
    [SerializeField] AudioMixer mixer       = null;
    [SerializeField] int        maxSounds   = 10;

    //Private
    private Dictionary<string, TrackInfo> tracks = new Dictionary<string, TrackInfo>();
    private List<LayeredAudioSource> layeredAudio = new List<LayeredAudioSource>();

    private List<AudioPoolItem> pool = new List<AudioPoolItem>();
    private Dictionary<ulong, AudioPoolItem> activePool = new Dictionary<ulong, AudioPoolItem>();
    private ulong idGiver = 0;
    private Transform listenerPos = null;


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

        for (int i = 0; i < maxSounds; i++)
        {
            //Create GameObject and assigned AudioSource and Parent 
            GameObject go = new GameObject("Pool Item");
            AudioSource audioSource = go.AddComponent<AudioSource>();
            go.transform.parent = transform;

            //Create and configure pool item 
            AudioPoolItem poolItem = new AudioPoolItem();
            poolItem.gameObj    = go;
            poolItem.audioSrc   = audioSource;
            poolItem.transf     = go.transform;
            poolItem.isPlaying  = false;

            go.SetActive(false); //Deactivate

            //Add it to the list 
            pool.Add(poolItem);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        listenerPos = FindObjectOfType<AudioListener>().transform;
    }

    private void Update()
    {
        foreach (LayeredAudioSource layeredSrc in layeredAudio)
        {
            if (layeredSrc != null)
            {
                layeredSrc.Update();
            }
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

    public IEnumerator PlayOneShotSoundDelayed(string _track, AudioClip _clip, Vector3 _pos, float _volume, float _duration, float _spatialBlend, float _priority = 128)
    {
        yield return new WaitForSeconds(_duration);

        PlayOneShotSound(_track, _clip, _pos, _volume, _spatialBlend, _priority);
    }

    public ulong PlayOneShotSound(string _track, AudioClip _clip, Vector3 _pos, float _volume, float _spatialBlend, float _priority = 128, float startTime = 0.0f)
    {
        if (!tracks.ContainsKey(_track) || _clip == null || _volume.Equals(0.0f))
            return 0;

        //Lower is the value, the more important is the sound (should be a value between 1 and 255)
        float unImportance = (listenerPos.position - _pos).sqrMagnitude / Mathf.Max(1, _priority);

        //Record least important sound from the pool
        int leastImportantIndex = -1;
        float leastImportantValue = float.MaxValue;
        for(int i = 0; i < pool.Count; i++)
        {
            AudioPoolItem poolItem = pool[i];
            if (!poolItem.isPlaying) //Return pool item if available
            {
                return ConfigurePoolObject(i, _track, _clip, _pos, _volume, _spatialBlend, unImportance, startTime);
            }
            else if(poolItem.unImportance > leastImportantValue) //Record least important sound
            {
                leastImportantIndex = i;
                leastImportantValue = poolItem.unImportance;
            }
        }

        //Check if the sound can be played (if the importance is less than the recorded importance from the pool)
        if(leastImportantValue > unImportance)
            return ConfigurePoolObject(leastImportantIndex, _track, _clip, _pos, _volume, _spatialBlend, unImportance, startTime);

        return 0;
    }

    public void StopOneShotSound(ulong id)
    {
        if (activePool.TryGetValue(id, out AudioPoolItem activeSound))
        {
            //Stop fade corutine
            StopCoroutine(activeSound.coroutine);

            //Stop sound
            activeSound.audioSrc.Stop();
            activeSound.isPlaying = false;

            activeSound.audioSrc.clip = null;
            activeSound.gameObj.SetActive(false);

            //Remove from pool
            activePool.Remove(id);
        }
    }

    protected IEnumerator StopSoundDelayed(ulong _id, float _duration)
    {
        yield return new WaitForSeconds(_duration);

        if (activePool.TryGetValue(_id, out AudioPoolItem activeSound))
        {
            //Stop sound
            activeSound.audioSrc.Stop();
            activeSound.isPlaying = false;

            activeSound.audioSrc.clip = null;
            activeSound.gameObj.SetActive(false);

            //Remove from pool
            activePool.Remove(_id);
        }
    }

    public void StopSound(ulong _id)
    {
        if (activePool.TryGetValue(_id, out AudioPoolItem activeSound))
        {
            //Stop sound
            activeSound.audioSrc.Stop();
            activeSound.isPlaying = false;

            activeSound.audioSrc.clip = null;
            activeSound.gameObj.SetActive(false);

            //Remove from pool
            activePool.Remove(_id);
        }
    }

    protected ulong ConfigurePoolObject(int _poolIndex, string _track, AudioClip _clip, Vector3 _pos, float _volume, float _spatialBlend, float _unImportance, float startTime)
    {
        if (_poolIndex < 0 || _poolIndex > pool.Count)
            return 0;

        AudioPoolItem poolItem = pool[_poolIndex];

        //"Generate" new ID so we can stop it later if we want
        idGiver++;

        //Configure the audio source's clip properties
        AudioSource source  = poolItem.audioSrc;
        source.clip         = _clip;
        source.volume       = _volume;
        source.spatialBlend = _spatialBlend;
        source.time         = Mathf.Min(startTime, source.clip.length - 0.01f);
        //Assign to requested audio group
        source.outputAudioMixerGroup = tracks[_track].group;
        //Position source at requested position
        source.transform.position = _pos;
        Debug.Log($"start time: {startTime}, Clip Length: {source.clip.length}");
        //Enable Gameobejct and record that it is now playing 
        poolItem.isPlaying      = true;
        poolItem.unImportance   = _unImportance;
        poolItem.ID             = idGiver;
        poolItem.gameObj.SetActive(true);
        //Play
            source.Play();

        //Coroutine to stop sound and put it back to the pool
        poolItem.coroutine = StopSoundDelayed(idGiver, source.clip.length);
        StartCoroutine(poolItem.coroutine);

        //Add this sound to our active pool with its unique ID
        activePool[idGiver] = poolItem;

        return idGiver;
    }

    public ILayeredAudioSource RegisterLayeredAudioSource(AudioSource source, int layers)
    {
        if(source != null && layers > 0)
        {
            //Check it's not the same source contained in the layeredAudio
            for (int i = 0; i < layeredAudio.Count; i++)
            {
                LayeredAudioSource item = layeredAudio[i];
                if(item != null)
                {
                    if (item.AudioSrc == source)
                        return item;
                }
            }

            //Create a new layered audio item and add it to the managed list
            LayeredAudioSource newLayeredAudio = new LayeredAudioSource(source, layers);
            layeredAudio.Add(newLayeredAudio);

            return newLayeredAudio;
        }

        return null;
    }

    public void UnregisterLayeredAudioSource(ILayeredAudioSource source)
    {
        layeredAudio.Remove((LayeredAudioSource)source);
    }

    public void UnregisterLayeredAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        //Check if there is a LayeredAudioSource with that source
        for (int i = 0; i < layeredAudio.Count; i++)
        {
            LayeredAudioSource item = layeredAudio[i];
            if (item != null)
            {
                if (item.AudioSrc == source)
                {
                    layeredAudio.Remove(item);
                    return;
                }
            }
        }
    }
}
