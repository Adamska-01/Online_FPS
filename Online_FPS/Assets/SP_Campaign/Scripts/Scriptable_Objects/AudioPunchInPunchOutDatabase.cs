using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioPunchInPunchOutInfo
{
    public AudioClip clip = null;
    public float startTime = 0.0f;
    public float endTime = 0.0f;
}

[CreateAssetMenu(fileName = "New Audio Punch-In Punch-Out Database")]
public class AudioPunchInPunchOutDatabase : ScriptableObject
{
    //Inspector-Assigned
    [SerializeField] protected List<AudioPunchInPunchOutInfo> dataList = new List<AudioPunchInPunchOutInfo>();

    //Internal 
    protected Dictionary<AudioClip, AudioPunchInPunchOutInfo> dataDictionary = new Dictionary<AudioClip, AudioPunchInPunchOutInfo>();

    public void OnEnable()
    {
        foreach (AudioPunchInPunchOutInfo info in dataList)
        {
            if(info.clip != null)
            {
                dataDictionary.Add(info.clip, info);
            }
        }
    }

    public AudioPunchInPunchOutInfo GetClipInfo(AudioClip clip)
    {
        if(dataDictionary.ContainsKey(clip))
        {
            return dataDictionary[clip];
        }

        return null;
    }
}
