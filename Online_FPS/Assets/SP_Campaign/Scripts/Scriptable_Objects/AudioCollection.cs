using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ClipBank
{
    public List<AudioClip> clips = new List<AudioClip>();
}


[CreateAssetMenu(fileName ="New Audio Collection")]
public class AudioCollection : ScriptableObject
{
    //Inspector Assigned 
    [SerializeField] private string audioGroup = string.Empty;

    [SerializeField, Range(0.0f, 1.0f)] private float volume = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float spatialBlend = 1.0f;
    [SerializeField, Range(0, 256)] private int priority = 128;

    [SerializeField] private List<ClipBank> audioClipBanks = new List<ClipBank>();

    //Properties
    public string AudioGroup    { get { return audioGroup; } }
    public float Volume         { get { return volume; } }
    public float SpatialBlend   { get { return spatialBlend; } }
    public int Priority         { get { return priority; } }
    public int BankCount        { get { return audioClipBanks.Count; } }
    public AudioClip RandomClip { get { return this[0]; } } //Easily access the first bank (usefull if there's only 1 bank)


    //Indexer to allow the use of '[]' notation on 'AudioCollection' and returns a random clip from 'audioClipBanks'
    public AudioClip this[int i]
    {
        get {
            if (audioClipBanks == null || audioClipBanks.Count <= i)
                return null;
            if (audioClipBanks[i].clips.Count == 0)
                return null;

            List<AudioClip> clipList = audioClipBanks[i].clips;
            AudioClip clip = clipList[Random.Range(0, clipList.Count)];

            return clip;
        }
    }
}
