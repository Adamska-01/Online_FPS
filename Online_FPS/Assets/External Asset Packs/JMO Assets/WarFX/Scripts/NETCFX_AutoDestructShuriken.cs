using UnityEngine;
using System.Collections;
using Photon.Pun;

[RequireComponent(typeof(ParticleSystem))]
public class NETCFX_AutoDestructShuriken : MonoBehaviour
{
	private PhotonView pv;
	public bool OnlyDeactivate;
	
	void OnEnable()
	{
		pv = GetComponent<PhotonView>();
		StartCoroutine("CheckIfAlive");
	}
	
	IEnumerator CheckIfAlive ()
	{
		while(true)
		{
			yield return new WaitForSeconds(0.5f);
			if(!GetComponent<ParticleSystem>().IsAlive(true))
			{
				if(OnlyDeactivate)
				{
					#if UNITY_3_5
						this.gameObject.SetActiveRecursively(false);
					#else
						this.gameObject.SetActive(false);
					#endif
				}
				else
					if(pv.IsMine) PhotonNetwork.Destroy(GetComponent<PhotonView>());
				break;
			}
		}
	}
}
