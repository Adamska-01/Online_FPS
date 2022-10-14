using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageIndicator : MonoBehaviour
{
    private const float MAX_TIMER = 4.0f;
    private float timer = MAX_TIMER;

    private CanvasGroup canvasGroup = null;
    protected CanvasGroup CanvasGroup
    {
        get
        {
            if(canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if(canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            return canvasGroup;
        }
    }

    private RectTransform rect = null;
    protected RectTransform Rect
    {
        get
        {
            if (rect == null)
            {
                rect = GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = gameObject.AddComponent<RectTransform>();
                }
            }
            return rect;
        }
    }

    public Transform target { get; protected set; } = null;
    private Transform player = null;

    private IEnumerator IE_Countdown = null;
    private Action unRegister = null;

    private Quaternion tRot = Quaternion.identity;
    private Vector3 tPos = Vector3.zero;

    public void Register(Transform _target, Transform _player, Action _unRegister)
    {
        //Register hit indicator and rotion based on target pos
        target = _target;
        player = _player;
        unRegister = _unRegister;

        StartCoroutine(RotateToTheTarget());
        StartTimer();
    }

    private void StartTimer()
    {
        if (IE_Countdown != null)
            StopCoroutine(IE_Countdown);

        IE_Countdown = Countdown();

        StartCoroutine(IE_Countdown);
    }

    public void Restart()
    {
        timer = MAX_TIMER;
        StartTimer();
    }

    private IEnumerator Countdown()
    {
        while(CanvasGroup.alpha <1.0f)
        {
            CanvasGroup.alpha += 4.0f * Time.deltaTime;
            yield return null;
        }

        while(timer > 0.0f)
        {
            --timer;
            yield return new WaitForSeconds(1);
        }

        while(CanvasGroup.alpha > 0.0f)
        {
            CanvasGroup.alpha -= 2.0f * Time.deltaTime;
            yield return null;
        }

        unRegister(); //Delete hit indicator
        Destroy(gameObject);
    }

    IEnumerator RotateToTheTarget()
    {
        while(enabled)
        {
            if(target != null)
            {
                tPos = target.position;
                tRot = target.rotation;
            }

            if (player != null)
            {
                Vector3 direction = player.position - tPos;

                //Rotate only on the z (2D rot)
                tRot = Quaternion.LookRotation(direction);
                tRot.z = -tRot.y;
                tRot.x = 0;
                tRot.y = 0;

                //Assign rect direction/rotation
                Vector3 northDirection = new Vector3(0.0f, 0.0f, player.eulerAngles.y); //this is our current rotation (y translated to z(screen))
                Rect.localRotation = tRot * Quaternion.Euler(northDirection);
            }
            else
                timer = 0.0f;

            yield return null;
        }
    }
}
