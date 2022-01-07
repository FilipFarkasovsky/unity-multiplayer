using UnityEngine;
using Multiplayer;

/// <summary> Controls animation of the player</summary>
public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;
    //public Transform weaponParent;
    //public Weapon weapon;

    private static int lateralSpeedHash;
    private static int forwardSpeedHash;
    private static int rifleAiming;
    private static int motionTimeHash;
    private static int jumpLayerIndex;

    private const float normalizedTimeSpeedMultiplier = 1.5f; // The regular animation is too slow so we will make it little bit faster

    //[SerializeField] public Weapon[] weapons;

    private void Awake()
    {
        lateralSpeedHash = Animator.StringToHash("LateralSpeed");
        forwardSpeedHash = Animator.StringToHash("ForwardSpeed");
        rifleAiming = Animator.StringToHash("RifleAiming");
        motionTimeHash = Animator.StringToHash("MotionTime");

        jumpLayerIndex = animator.GetLayerIndex("Jump");

        //EquipWeapon(weapons[0]);
    }

    public void UpdateAnimatorProperties(AnimationData animationData)
    {
        animator.SetFloat(lateralSpeedHash, animationData.lateralSpeed);
        animator.SetFloat(forwardSpeedHash, animationData.forwardSpeed);
        animator.SetFloat(rifleAiming, animationData.rifleAmount);
        animator.SetLayerWeight(jumpLayerIndex, animationData.jumpLayerWeight);
        animator.SetFloat(motionTimeHash, animationData.normalizedTime * normalizedTimeSpeedMultiplier);
    }

    public void UpdateAnimatorProperties(float _lateralSpeed, float _forwardSpeed, float jumpLayerWeight, float normalizedTime, float _aimingAmount)
    {
        animator.SetFloat(lateralSpeedHash, _lateralSpeed);
        animator.SetFloat(forwardSpeedHash, _forwardSpeed);
        animator.SetFloat(rifleAiming, _aimingAmount);
        animator.SetLayerWeight(jumpLayerIndex, jumpLayerWeight);
        animator.SetFloat(motionTimeHash, normalizedTime * normalizedTimeSpeedMultiplier);
    }

    //public void EquipWeapon(Weapon newWeapon)
    //{
    //    if (weapon)
    //    {
    //        weapon.gameObject.SetActive(false);
    //    }

    //    if (animator) animator.SetFloat(rifleAiming, 1);

    //    weapon = newWeapon;
    //    weapon.gameObject.SetActive(true);
    //    weapon.transform.parent = weaponParent;
    //    weapon.transform.localPosition = Vector3.zero;
    //    weapon.transform.localRotation = Quaternion.identity;
    //}
}
