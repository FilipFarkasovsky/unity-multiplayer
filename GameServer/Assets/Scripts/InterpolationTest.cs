using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterpolationTest : Multiplayer.ServerNetworkedEntity
{
    private Vector3 startPosition;
    private Multiplayer.LogicTimer logicTimer;

    // Start is called before the first frame update
    void Awake()
    {
        startPosition = transform.position;
    }
    private void Start()
    {
        logicTimer = new Multiplayer.LogicTimer(FixedTime);
        logicTimer.Start();
    }
    private void Update()
    {
        logicTimer.Update();
    }
    private void FixedTime()
    {
        transform.rotation = Quaternion.Euler(Vector3.up * Time.time * 50f);
        transform.position = startPosition + new Vector3(0f, Mathf.Sin(3 * Time.unscaledTime) * 3, 0f);
    }
}
