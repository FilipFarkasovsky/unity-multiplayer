using System.Collections;
using UnityEngine;

/// <summary> Controls visual effects of the weapon </summary>
public class Weapon : MonoBehaviour
{
    [Header("Raycast")]
    public GameObject playerCamera;

    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem hitEffect;
    private ushort fireRate = 8;
    private ushort muzzleFlashRate = 16;

    [Header("Sound effects")]
    public AudioSource audioSource;
    public AudioClip gunFire;
    public AudioClip hitSound;
    public AudioClip killSound;

    [Header("Hitmarker UI")]
    public UnityEngine.UI.Image hitmarkerImage;
    public float showTime;

    
    private float accumulatedTime = 0;
    private Ray ray;
    private RaycastHit hitInfo;

    [SerializeField] bool isLocalPlayer;

    private void Start()
    {
        audioSource.volume = 0.2f;
    }

    public void FireBullet(){
        //Muzzle Flash
        muzzleFlash.Emit(1);
        audioSource.PlayOneShot(gunFire);


        // Hit and trail effect
        if (isLocalPlayer)
        {
            ray.origin = playerCamera.transform.position;
            ray.direction = playerCamera.transform.forward;
            if (Physics.Raycast(ray, out hitInfo))
            {
                hitEffect.transform.position = hitInfo.point;
                hitEffect.transform.forward = hitInfo.normal;
                hitEffect.Emit(1);
            }
        }
    }

    public void GetHitmarker(){
        StopCoroutine("showhitmarker");
        audioSource.PlayOneShot(hitSound);
        hitmarkerImage.enabled = true;
        StartCoroutine("showhitmarker");
    }

    private IEnumerator showhitmarker(){
        yield return new WaitForSeconds(showTime);
        hitmarkerImage.enabled = false;
    }
}
