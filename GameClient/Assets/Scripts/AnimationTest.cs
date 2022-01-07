using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTest : MonoBehaviour
{
    public Animator animator;

    private static int lateralSpeedHash;
    private static int forwardSpeedHash;
    private static int rifleAiming;
    private static int motionTimeHash;


    [Header("Animation Properties")]
    public float lateralSpeed;
    public float forwardSpeed;
    public float currentLayerWeight;

    public float normalizedTime;
    public float isAiming;

    private static int jumpLayerIndex;
    public bool ground;
    public float smoothJumpTime;
    private float currentLayerWeightVelocity;

    private void Start()
    {
        lateralSpeedHash = Animator.StringToHash("LateralSpeed");
        forwardSpeedHash = Animator.StringToHash("ForwardSpeed");
        rifleAiming = Animator.StringToHash("RifleAiming");
        motionTimeHash = Animator.StringToHash("MotionTime");

        jumpLayerIndex = animator.GetLayerIndex("Jump");
    }

    void FixedUpdate()
    {

        currentLayerWeight = animator.GetLayerWeight(jumpLayerIndex);
        currentLayerWeight = Mathf.SmoothDamp(currentLayerWeight, ground ? 0 : 1, ref currentLayerWeightVelocity, smoothJumpTime, Mathf.Infinity, Time.fixedDeltaTime);
        normalizedTime = 1.5f * Time.time;

        animator.SetLayerWeight(jumpLayerIndex, currentLayerWeight);
        animator.SetFloat(lateralSpeedHash, lateralSpeed);
        animator.SetFloat(forwardSpeedHash, forwardSpeed);
        animator.SetFloat(rifleAiming, isAiming);
        animator.SetFloat(motionTimeHash, normalizedTime);
    }
}
