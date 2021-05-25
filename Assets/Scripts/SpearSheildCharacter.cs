using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpearSheildCharacter : CollisionObject
{
    public float gravity;
    public float maxDistanceToSeePlayer;
    public float spearMoveSpeed;
    public float walkMoveSpeed;
    public float maxAttackDistance;
    public float patrolWalkTime;
    public float patrolIdleTime;
    public float eyeHeight;

    private enum AIState
    {
        Idle,
        Walking,
        Attacking,
        Shielding,
        Turning // note that turning functions a little weird as it's both a state and a transition, which I did since it transitions back into the animation it was coming from
    }

    private AIState state;
    private AIState lastState;
    private Animator anim;
    private bool transitioning;
    private Transform player;
    private bool playerSeen;
    private Vector2 distanceToPlayer;
    private float characterDirection; // 1.0 for right, -1.0 for left
    private float patrolTimer;
    private bool restartPatrol;
    private const float blockChance = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        state = AIState.Idle;
        lastState = AIState.Idle;
        anim = transform.GetChild(0).GetComponent<Animator>();
        transitioning = false;
        player = GameObject.FindWithTag("Player").transform;
        playerSeen = false;
        characterDirection = 1.0f;
        restartPatrol = true;
        patrolTimer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        AIBrain();
        UpdateAnimations();
    }

    private void AIBrain()
    {
        if(!transitioning) { // none of the transition animations require movement, so I just skip over the AI processes when transitioning
            LookForPlayer();
            if (playerSeen) {
                restartPatrol = true;
                AIFighting();
            } else {
                AIPatrolling();
            }
        }
    }


    // this function checks if the player is close enough and that the enemy has line of sight
    private void LookForPlayer()
    {
        float distance = maxDistanceToSeePlayer + 1;
        if (getDistanceToPlayer())
        {
            distance = distanceToPlayer.magnitude;
        }
        if (distance <= maxDistanceToSeePlayer)
        {
            playerSeen = true;
        } else {
            playerSeen = false;
        }
    }

    private bool getDistanceToPlayer()
    {
        if (Physics2D.Linecast(transform.position + (Vector3.up * eyeHeight), player.position)) {
            return false;
        }
        distanceToPlayer = new Vector2(player.position.x - transform.position.x, player.position.y - transform.position.x);
        return true;
    }

    private void AIFighting()
    {
        if (state == AIState.Walking) {
            if (distanceToPlayer.x * characterDirection < maxAttackDistance) {
                ChangeState(AIState.Idle);
            }
        } else if (state == AIState.Idle) {
            if (distanceToPlayer.x * characterDirection >= maxAttackDistance) {
                ChangeState(AIState.Walking);
            } else if (distanceToPlayer.x * characterDirection >= 0) { // if player is in front of enemy attack or block
                if ((lastState == AIState.Attacking || lastState == AIState.Turning) && Random.value < blockChance) { // block if you either just attacked or just turned around + 1/2 chance rng, otherwise attack
                    ChangeState(AIState.Shielding);                                                                   // this means that they'll rarely ever block twice in a row (only if you're repeatedly pogoing back and forth over them)
                } else {                                                                                              // meaning that if you're just strafing back and forth to fight them you won't have the potential of infinitely smacking their sheild.
                    ChangeState(AIState.Attacking);                                                                   // I'd of course have to playtest this to see if it actually works well, of course, but I'm not aware of how to do that yet.
                }
            } else { // if player is behind enemy, turn around
                ChangeState(AIState.Turning);
            }
        } else if (state == AIState.Shielding) { // this can also be exited as the animation ends, but will end early if the player jumps behind the enemy.
            if(distanceToPlayer.x * characterDirection < 0) {
                EndShield();
            }
        } // note that attacking is handled entirely by animation events
    }

    private void AIPatrolling()
    {
        if (restartPatrol) { // each loop starts with Idle
            restartPatrol = false;
            patrolTimer = patrolIdleTime;
            if (state != AIState.Idle) {
                ChangeState(AIState.Idle);
            }
        } else if (state == AIState.Idle) { // then Walking
            if (patrolTimer >= 0) {
                patrolTimer -= Time.deltaTime;
            } else {
                patrolTimer = patrolWalkTime;
                ChangeState(AIState.Walking);
            }
        } else if (state == AIState.Walking) { // then Turning, and starting over
            if (patrolTimer >= 0) {
                patrolTimer -= Time.deltaTime;
            } else {
                ChangeState(AIState.Turning);
                restartPatrol = true;
            }
        } else if (state == AIState.Shielding) { // end shielding early if the player leaves line of sight
            ChangeState(AIState.Idle);
            restartPatrol = true;
        }
    }

    private void UpdateAnimations()
    {
        anim.SetBool("Idle", state == AIState.Idle);
        anim.SetBool("Walking", state == AIState.Walking);
        anim.SetBool("Attacking", state == AIState.Attacking);
        anim.SetBool("Shielding", state == AIState.Shielding);
        anim.SetBool("Turning", state == AIState.Turning);
    }

    // communicating movements to the collision system
    void FixedUpdate()
    {
        velocity.y -= gravity * Time.deltaTime;
        velocity.x = targetVelocity.x;
        HandleMovements();
    }

    // communicating movements to the collision system
    private void HandleMovements()
    {
        Vector2 deltaPosition = velocity * Time.deltaTime;
        Vector2 move = Vector2.right * deltaPosition.x;
        Movement(move, false);
        move = Vector2.up * deltaPosition.y;
        Movement(move, true);
    }

    private void ChangeState(AIState newState) {
        lastState = state;
        state = newState;
        transitioning = true;
    }

    // these are used for callback from the animations
    public void TurnCharacter() {
        transform.Rotate(0.0f, 180.0f, 0.0f);
        lastState = state;
        state = AIState.Idle;
        transitioning = false;
        characterDirection *= -1.0f;
    }

    public void EndAttack() {
        ChangeState(AIState.Idle);
    }

    public void EndShield() {
        ChangeState(AIState.Idle);
    }

    public void StopBasicTransition() {
        transitioning = false;
    }

    public void SetTargetVelocity(float v) { // velocity is set via the animations, which I did to pinpoint the movements on the dash attack.
        targetVelocity.x = v * characterDirection;
    }
}
