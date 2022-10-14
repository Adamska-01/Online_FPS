using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

public abstract class NETItem : MonoBehaviour
{ 
    public ItemInfo itemInfo;
    public GameObject[] itemObject;

    public abstract bool Use();
    public abstract bool CanReload();
    public abstract void Reload();
}
