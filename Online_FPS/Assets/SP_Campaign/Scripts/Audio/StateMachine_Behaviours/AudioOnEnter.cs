using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioOnEnter : StateMachineBehaviour
{
    [SerializeField] AudioCollection audioCollection = null;
    [SerializeField] int bank = 0;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (AudioManager.Instance == null || audioCollection == null)
            return;

        AudioManager.Instance.PlayOneShotSound(audioCollection.AudioGroup,
                                               audioCollection[bank],
                                               animator.transform.position,
                                               audioCollection.Volume,
                                               audioCollection.SpatialBlend,
                                               audioCollection.Priority);
    }
}
