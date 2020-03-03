using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppearEffect : MonoBehaviour
{
    public float Duration = 0.25f;
    public GameObject CompleteParticles;
    public float InitialDelay = 0;
    
    IEnumerator Start()
    {
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(InitialDelay);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Duration;
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;

        if (CompleteParticles != null)
        {
            var particles = Instantiate(CompleteParticles,transform.position,Quaternion.identity);
            Destroy(particles, 5);
        }
    }
}
