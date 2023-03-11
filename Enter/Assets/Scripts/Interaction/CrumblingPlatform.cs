using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingPlatform : MonoBehaviour
{
    Renderer renderer;
    float preCrumblingDuration = 0.5f;
    float crumblingDuration = 3.0f;
    Color originalColor = Color.white;
    Color crumblingColor = Color.red;
    // [SerializedField] private EdgeCollider2D edgeCollider;
    EdgeCollider2D edgeCollider;

    // Start is called before the first frame update
    void Start()
    {
        this.renderer = GetComponent<Renderer>();
        this.edgeCollider = GetComponent<EdgeCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {

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

        this.renderer.material.color = originalColor;
        GetComponent<EdgeCollider2D>().enabled = true;
        yield return null;
    }
}
