using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{ 
    private Animator controller = null;

    //Animation parameters hash 
    int horizontalHash;
    int verticalHash;
    int attackHash;

    const float maxHorizontal = 2.32f;
    const float maxVertical = 5.667774f;

    void Start()
    {
        controller = GetComponent<Animator>();

        //Set animation parameters
        horizontalHash = Animator.StringToHash("Horizontal");
        verticalHash = Animator.StringToHash("Vertical");
        attackHash = Animator.StringToHash("Attack");
    }

    void Update()
    {
        float xAxis = Input.GetAxis("Horizontal") * maxHorizontal;
        float yAxis = Input.GetAxis("Vertical") * maxVertical;
       
        //Set locomotion parameters
        controller.SetFloat(horizontalHash, xAxis, 0.1f, Time.deltaTime);
        controller.SetFloat(verticalHash, yAxis, 1.0f, Time.deltaTime);

        //Set Attack trigger
        if(Input.GetMouseButtonDown(0))
            controller.SetTrigger(attackHash);
    }
}
