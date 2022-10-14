using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

 
public class SoundManager : MonoBehaviour
{
    const float MUSIC_VOLUME = 0.2f;
    private List<AudioSource> currentMusicsPlaying = new List<AudioSource>();
    public List<AudioClip> musics = new List<AudioClip>();
    public static SoundManager instance; 
    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        instance = this;

        //Create Dictionary
        for (int i = 0; i < clipName.Count; i++)
            clipLib.Add(clipName[i], clipList[i]);

        CreateInstances();
    }

    public AudioMixer mixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Dictionary Settings")]
    public List<SoundManagerConstants.Clips> clipName = new List<SoundManagerConstants.Clips>();
    public List<AudioClip> clipList = new List<AudioClip>();
    private Dictionary<SoundManagerConstants.Clips, AudioClip> clipLib = new Dictionary<SoundManagerConstants.Clips, AudioClip>();

    [Header("Pool Settings")]
    public GameObject prefabToPool;
    public int amountToPool;
    private List<GameObject> pooledPrefabs = new List<GameObject>();


    public void PlaySound(SoundManagerConstants.Clips clip, SoundManagerConstants.AudioOutput group, Vector3 position, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null) 
            return;

        prefab.transform.position = position;
        prefab.SetActive(true);

        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clipLib[clip];
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play(); 

        StartCoroutine(BackToPool(prefab, clipLib[clip].length));
    }

    public AudioSource PlaySoundAndReturn(SoundManagerConstants.Clips clip, SoundManagerConstants.AudioOutput group, Vector3 position, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null)
            return null;

        prefab.transform.position = position;
        prefab.SetActive(true);

        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clipLib[clip];
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play();

        StartCoroutine(BackToPool(prefab, clipLib[clip].length));

        return prefabAudioSource;
    }

    public void PlaySound(AudioClip clip, SoundManagerConstants.AudioOutput group, Vector3 position, out float duration, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null)
        {
            duration = 0;
            return;
        }

        prefab.transform.position = position;
        prefab.SetActive(true);

        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clip;
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play();

        StartCoroutine(BackToPool(prefab, clip.length));

        duration = clip.length;
    }

    public void PlaySound(AudioClip clip, SoundManagerConstants.AudioOutput group, Vector3 position, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null) return;

        prefab.transform.position = position;
        prefab.SetActive(true);

        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clip;
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play();

        StartCoroutine(BackToPool(prefab, clip.length));
    }

    public void PlaySound(SoundManagerConstants.Clips clip, SoundManagerConstants.AudioOutput group, GameObject parent, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null) return;

        #region Configure Prefab
        prefab.transform.position = parent.transform.position;
        prefab.transform.parent = parent.transform;
        prefab.SetActive(true);
        #endregion

        #region Configure AudioSource
        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clipLib[clip];
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play();
        #endregion

        StartCoroutine(BackToPool(prefab, clipLib[clip].length, true));
    }

    public AudioSource PlaySoundAndReturn(SoundManagerConstants.Clips clip, SoundManagerConstants.AudioOutput group, GameObject parent, float volume = 1)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null) 
            return null;

        #region Configure Prefab
        prefab.transform.position = parent.transform.position;
        prefab.transform.parent = parent.transform;
        prefab.SetActive(true);
        #endregion

        #region Configure AudioSource
        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clipLib[clip];
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = group == SoundManagerConstants.AudioOutput.SFX ? sfxGroup : musicGroup;
        prefabAudioSource.Play();
        #endregion

        StartCoroutine(BackToPool(prefab, clipLib[clip].length, true));

        return prefabAudioSource;
    }

    private void SetMusicsPlaying()
    { 
        for (int i = 0; i < musics.Count; i++)
        {
            GameObject prefab = GetPoolObject();
            if (prefab == null)
                continue;
             
            prefab.SetActive(true);

            currentMusicsPlaying.Add(prefab.GetComponent<AudioSource>());
            currentMusicsPlaying[i].outputAudioMixerGroup = musicGroup;
            currentMusicsPlaying[i].clip = musics[i];
            currentMusicsPlaying[i].spatialBlend = 0.0f;
            currentMusicsPlaying[i].priority = 256; //low
            currentMusicsPlaying[i].volume = 0.0f; 
            currentMusicsPlaying[i].loop = true; 
            currentMusicsPlaying[i].Play(); 
        } 
    }

    public void PlayRandomMusicFromList()
    {
        if (currentMusicsPlaying.Count <= 0)
            SetMusicsPlaying();

        int musicPlayingIndex = -1;
        for (int i = 0; i < currentMusicsPlaying.Count; i++)
        {
            if (currentMusicsPlaying[i].volume > 0)
            {
                musicPlayingIndex = i;
                break;
            }
        }

        if (musicPlayingIndex == -1)
        { 
            int musicIndex = Random.Range(0, currentMusicsPlaying.Count);
            currentMusicsPlaying[musicIndex].volume = MUSIC_VOLUME;
        }
        else
        {
            int musicIndex = Random.Range(0, currentMusicsPlaying.Count);
            for (int i = 0; i < currentMusicsPlaying.Count; i++)
            {
                if (currentMusicsPlaying[i].volume > 0)
                {
                    StartCoroutine(FadeOutSong(currentMusicsPlaying[i], musicIndex));
                    break;
                }
            }
        }
    }

    public void FadeOutAllMusic(float timeToFade)
    {
        for (int i = 0; i < currentMusicsPlaying.Count; i++)
        {
            if (currentMusicsPlaying[i].volume > 0)
            {
                StartCoroutine(FadeOutAllMusic(currentMusicsPlaying[i], timeToFade));
                break;
            }
        }
    }

    private IEnumerator FadeOutAllMusic(AudioSource src, float time)
    {
        float timeToFade = time;
        float timeElapsed = 0.0f;
        float startVl = src.volume;
        while (timeElapsed < timeToFade)
        {
            src.volume = Mathf.Lerp(startVl, 0, timeElapsed / timeToFade);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        src.volume = 0.0f;
    }

    private IEnumerator FadeOutSong(AudioSource src, int musicInx)
    {
        float timeToFade = 0.25f;
        float timeElapsed = 0.0f;
        float startVl = src.volume;
        while (timeElapsed < timeToFade)
        {
            src.volume = Mathf.Lerp(startVl, 0, timeElapsed / timeToFade);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        src.volume = 0.0f;

        timeElapsed = 0.0f;
        while (timeElapsed < timeToFade)
        {
            currentMusicsPlaying[musicInx].volume = Mathf.Lerp(0, startVl, timeElapsed / timeToFade);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        currentMusicsPlaying[musicInx].volume = MUSIC_VOLUME;
    }

    private AudioSource PlayMusic(SoundManagerConstants.Clips clip, float volume)
    {
        GameObject prefab = GetPoolObject();

        if (prefab == null)
            return null;
         
        prefab.SetActive(true);

        #region Configure AudioSource
        AudioSource prefabAudioSource = prefab.GetComponent<AudioSource>();

        prefabAudioSource.clip = clipLib[clip];
        prefabAudioSource.volume = volume;
        prefabAudioSource.outputAudioMixerGroup = musicGroup;
        prefabAudioSource.Play();
        #endregion

        return prefabAudioSource;
    }

    private void CreateInstances()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            GameObject go = Instantiate(prefabToPool, gameObject.transform);
            go.SetActive(false);
            pooledPrefabs.Add(go);
        }
    }

    private GameObject GetPoolObject()
    {
        for (int i = 0; i < pooledPrefabs.Count; i++)
        {
            //Sometimes the object is destroyed
            if (pooledPrefabs[i] == null)
            {
                pooledPrefabs.Remove(pooledPrefabs[i]);

                GameObject go = Instantiate(prefabToPool, gameObject.transform);
                go.SetActive(false);
                pooledPrefabs.Add(go);
                
                continue;
            }

            if (pooledPrefabs[i].gameObject.transform.parent == gameObject.transform)
                if (!pooledPrefabs[i].gameObject.activeInHierarchy)
                    return pooledPrefabs[i];
        }

        return null;
    }

    public void DisablePool()
    {
        foreach (GameObject prefab in pooledPrefabs)
        {
            prefab.transform.parent = gameObject.transform;
            prefab.SetActive(false);
        }
    }

    IEnumerator BackToPool(GameObject prefab, float seconds, bool unparent = false)
    {
        yield return new WaitForSeconds(seconds);

        if(!prefab.GetComponent<AudioSource>().loop)
        {
            if (unparent)
            {
                prefab.transform.parent = gameObject.transform;
            }

            prefab.SetActive(false);
        }
    }


    //Set Mixer Volumes
    public void SetSFXVolume(float sfxVol)
    {
        mixer.SetFloat("sfxVol", sfxVol);
    }

    public void SetMusicVolume(float musicVol)
    {
        mixer.SetFloat("musicVol", musicVol);
    }
}


public class SoundManagerConstants
{
    public enum AudioOutput
    {
        SFX,
        MUSIC
    }

    public enum Clips
    {
        RIFLE_SHOOT,
        HANDGUN_SHOOT,
        KNIFE_SWING,
        RIGHT_FOOTSTEP,
        RIGHT_FOOTSTEP_RUN,
        LEFT_FOOTSTEP,
        LEFT_FOOTSTEP_RUN,
        LANDING,
        DEATH,
        RELOAD_RIFLE,
        RELOAD_HANDGUN,
        DUMMY_SPAWN,
        DUMMY_DEATH, 
        MUSIC_TRACK_1,
        MUSIC_TRACK_2,
        MUSIC_TRACK_3,
        MUSIC_TRACK_4,
        MUSIC_TRACK_5,        
        BUTTON_SELECT,
        BUTTON_CLOSE,
        EMPTY_CLIP_HANDGUN,
        EMPTY_CLIP_RIFLE,
    }
}