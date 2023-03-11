using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingPlatform : MonoBehaviour
{
    Renderer renderer;
    float preCrumblingDuration = 0.5f;
    float crumblingDuration = 3.0f;
    float postCrumblingDuration = 1f;
    Color originalColor = Color.white;
    Color crumblingColor = Color.red;
    EdgeCollider2D edgeCollider;

    void Start()
    {
        this.renderer = GetComponent<Renderer>();
        this.edgeCollider = GetComponent<EdgeCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If player touches edge collider trigger
        if (other.gameObject.tag == "Player")
        {
            StartCoroutine(this.Crumble());
        }
    }

    IEnumerator Crumble()
    {
        // for now changing the object color into red
        yield return new WaitForSeconds(this.preCrumblingDuration);
        this.renderer.material.color = crumblingColor;
        GetComponent<EdgeCollider2D>().enabled = false;
        yield return new WaitForSeconds(this.crumblingDuration);
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        Debug.Log("Platform dies.");
        yield return new WaitForSeconds(this.postCrumblingDuration);
        Debug.Log("Platform returns.");
        this.renderer.material.color = originalColor;
        GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<EdgeCollider2D>().enabled = true; //what if platform regenerates before then?
        Debug.Log("Fixed.");
        yield return null;
    }
}
