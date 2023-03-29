using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingPlatform : MonoBehaviour
{
  Renderer _renderer;
  
  Color originalColor = Color.white;
  Color crumblingColor = Color.red;

  [SerializeField] private float preCrumblingDuration = 0.5f;
  [SerializeField] private float postCrumblingDuration = 1f;
  [SerializeField] private float crumblingDuration = 3.0f;
  [SerializeField] private float edgeColliderYOffset = 0.05f;

  private Vector2 _topLeft_local        => Vector2.zero + Vector2.up / 2.0f + Vector2.left / 2.0f;
  private Vector2 _topRight_local       => Vector2.zero + Vector2.up / 2.0f + Vector2.right / 2.0f;
  private Vector2 _topLeft_global       => transform.TransformPoint(_topLeft_local);
  private Vector2 _topRight_global      => transform.TransformPoint(_topRight_local);



  void Start()
  {
    this._renderer = GetComponentInChildren<Renderer>();
    this.setupEdgeColliderPoints();
  }

  void setupEdgeColliderPoints()
  {
    // setup the edge collider at a fixed offset based on edgeColliderYOffset
    EdgeCollider2D edgeCollider = GetComponent<EdgeCollider2D>();
    
    Vector2 edgeCollliderTopLeft_global   = _topLeft_global;
    Vector2 edgeCollliderTopRight_global  = _topRight_global;

    edgeCollliderTopLeft_global.y += edgeColliderYOffset;
    edgeCollliderTopRight_global.y += edgeColliderYOffset;

    List<Vector2> edgeColliderPoints = new List<Vector2>();
    edgeColliderPoints.Add(transform.InverseTransformPoint(edgeCollliderTopLeft_global));
    edgeColliderPoints.Add(transform.InverseTransformPoint(edgeCollliderTopRight_global));

    edgeCollider.points = edgeColliderPoints.ToArray();
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
    this._renderer.material.color = crumblingColor;
    GetComponent<EdgeCollider2D>().enabled = false;
    yield return new WaitForSeconds(this.crumblingDuration);
    GetComponent<BoxCollider2D>().enabled = false;
    GetComponent<SpriteRenderer>().enabled = false;
    Debug.Log("Platform dies.");
    yield return new WaitForSeconds(this.postCrumblingDuration);
    Debug.Log("Platform returns.");
    this._renderer.material.color = originalColor;
    GetComponent<BoxCollider2D>().enabled = true;
    GetComponent<SpriteRenderer>().enabled = true;
    GetComponent<EdgeCollider2D>().enabled = true; //what if platform regenerates before then?
    Debug.Log("Fixed.");
    yield return null;
  }
}
