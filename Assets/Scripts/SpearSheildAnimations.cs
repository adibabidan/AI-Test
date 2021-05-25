using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpearSheildAnimations : MonoBehaviour
{
    public SpearSheildCharacter parent;
    public float targetVelocity;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SendTargetVelocity();
    }

    public void TurnCharacter() {
        parent.TurnCharacter();
    }

    public void EndAttack() {
        parent.EndAttack();
    }

    public void EndShield() {
        parent.EndShield();
    }

    public void StopBasicTransition() {
        parent.StopBasicTransition();
    }

    private void SendTargetVelocity() {
        parent.SetTargetVelocity(targetVelocity);
    }
}
