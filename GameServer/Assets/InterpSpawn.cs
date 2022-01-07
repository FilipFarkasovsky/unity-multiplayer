using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterpSpawn : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Multiplayer.ServerNetworkedEntity.Spawn(NetworkedObjectType.projectile, transform.position);
    }
}
