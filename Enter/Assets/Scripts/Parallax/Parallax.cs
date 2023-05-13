using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Enter 
{
  public class Parallax : MonoBehaviour
  {
    public static Parallax Instance;
    
    [SerializeField] private Image _bgImageBackTop;
    [SerializeField] private Image _bgImageMidTop;
    [SerializeField] private Image _bgImageFrontTop;
    
    [SerializeField] private Image _bgImageBack;
    [SerializeField] private Image _bgImageMid;
    [SerializeField] private Image _bgImageFront;

    private Camera _camera => Camera.main;

    // Getters for canvas image positions
    private RectTransform frontTop => _bgImageFrontTop.GetComponent<RectTransform>();
    private RectTransform midTop   => _bgImageMidTop.GetComponent<RectTransform>();
    private RectTransform backTop  => _bgImageBackTop.GetComponent<RectTransform>();
    private RectTransform front    => _bgImageFront.GetComponent<RectTransform>();
    private RectTransform mid      => _bgImageMid.GetComponent<RectTransform>();
    private RectTransform back     => _bgImageBack.GetComponent<RectTransform>();

    // Note: Initial offset for the current backgrounds was measured from the middle position...
    private float initialFrontTopOffset;
    private float initialMidTopOffset;
    private float initialBackTopOffset;
    private float initialFrontOffset;
    private float initialMidOffset;
    private float initialBackOffset;

    // ... Which is why an initial offset of half the max height is needed
    private float initialCameraOffset;
    
    private float x1;
    private float x2;
    private float x3;
    private float h = 720;

    [SerializeField] private float aMax = 150; // World space units of highest camera altitude, found via playtesting and might change later
    [SerializeField] private float aMin = 0;  // World space units of lowest camera altitude
    
    [field:SerializeField] private float a => _camera.transform.position.y - initialCameraOffset;

    [field:SerializeField] private float x3Offset => -((a - aMin) / aMax) * (x3 - h);
    [field:SerializeField] private float x2Offset => -((a - aMin) / aMax) * (x2 - h);
    [field:SerializeField] private float x1Offset => -((a - aMin) / aMax) * (x1 - h);


    #region ================== Methods

    private void Awake()
    {
      if (Instance)
      {
        Destroy(gameObject);
        return;
      } 

      Instance = this;
      transform.SetParent(null);
      DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
      initialFrontTopOffset = frontTop.anchoredPosition.y;
      initialMidTopOffset   = midTop.anchoredPosition.y;
      initialBackTopOffset  = backTop.anchoredPosition.y;
      initialFrontOffset    = front.anchoredPosition.y;
      initialMidOffset      = mid.anchoredPosition.y;
      initialBackOffset     = back.anchoredPosition.y;
      x1 = initialFrontTopOffset - initialFrontOffset;
      x2 = initialMidTopOffset - initialMidOffset;
      x3 = initialBackTopOffset - initialBackOffset;
      initialCameraOffset = aMax / 2; // set the initial offset of the backgrounds to half the max height
    }

    void LateUpdate()
    {
      updateLayersHorizontal();
      updateLayersVertical();
    }

    #endregion

    #region ================== Helpers

    private void updateLayersHorizontal()
    {
      float x = _camera.transform.position.x;
      Vector2 backOffset  = new Vector2(x / 1000, 0);
      Vector2 midOffset   = new Vector2(x / 500,  0);
      Vector2 frontOffset = new Vector2(x / 250,  0);

      _bgImageBackTop.material.mainTextureOffset  = backOffset;
      _bgImageMidTop.material.mainTextureOffset   = midOffset;
      _bgImageFrontTop.material.mainTextureOffset = frontOffset;

      _bgImageBack.material.mainTextureOffset  = backOffset;
      _bgImageMid.material.mainTextureOffset   = midOffset;
      _bgImageFront.material.mainTextureOffset = frontOffset;
    }

    private void updateLayersVertical()
    {
      // Debug.Log("Camera y: " + _camera.transform.position.y);
      frontTop.anchoredPosition = new Vector2(frontTop.anchoredPosition.x,  initialFrontTopOffset + x1Offset);
      midTop.anchoredPosition   = new Vector2(midTop.anchoredPosition.x,    initialMidTopOffset   + x2Offset);
      backTop.anchoredPosition  = new Vector2(backTop.anchoredPosition.x,   initialBackTopOffset  + x3Offset);
      front.anchoredPosition    = new Vector2(front.anchoredPosition.x,     initialFrontOffset    + x1Offset);
      mid.anchoredPosition      = new Vector2(mid.anchoredPosition.x,       initialMidOffset      + x2Offset);
      back.anchoredPosition     = new Vector2(back.anchoredPosition.x,      initialBackOffset     + x3Offset);
    }

    #endregion
  }
}