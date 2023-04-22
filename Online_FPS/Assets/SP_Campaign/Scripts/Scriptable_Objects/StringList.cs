using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Custom Animation Curve")]
public class StringList : ScriptableObject
{
    [SerializeField] private List<string> stringList = new List<string>();


    public string this[int i]
    {
        get
        {
            if(i < stringList.Count)
            {
                return stringList[i];
            }

            return null;
        }
    }

    public int Count
    {
        get
        {
            return stringList.Count;
        }
    }
}
