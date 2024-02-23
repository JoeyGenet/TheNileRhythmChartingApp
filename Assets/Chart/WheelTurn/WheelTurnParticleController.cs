using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelTurnParticleController : MonoBehaviour
{
    [SerializeField] ParticleSystem particles;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (particles.particleCount == 0 && particles.gameObject.transform.parent == null)
        {
            Destroy(gameObject);
        }
    }
}
