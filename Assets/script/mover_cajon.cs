using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public gameObject Cajon1;
public class mover_cajon : MonoBehaviour
{
    // Start is called before the first frame update
    void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Cajon1"))
        {
            
        }
    }
}
