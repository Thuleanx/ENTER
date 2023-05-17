using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Enter 
{
  // [ExecuteAlways]
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
    
    private float x1;
    private float x2;
    private float x3;
    private float h = 720; // seems sus

    [SerializeField] private float aMax; // World space units of highest camera altitude, found via playtesting and might change later
    [SerializeField] private float aMin; // World space units of lowest camera altitude
    [SerializeField] public float AInitial; // For adjusting :)
    private float aRange => aMax - aMin;

    [field:SerializeField] private float a => _camera.transform.position.y - aMax / 2 + AInitial;

    [field:SerializeField] private float x3Offset => -((a - aMin) / aRange) * (x3 - h);
    [field:SerializeField] private float x2Offset => -((a - aMin) / aRange) * (x2 - h);
    [field:SerializeField] private float x1Offset => -((a - aMin) / aRange) * (x1 - h);

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

      initialFrontTopOffset = frontTop.anchoredPosition.y;
      initialMidTopOffset   =   midTop.anchoredPosition.y;
      initialBackTopOffset  =  backTop.anchoredPosition.y;
      initialFrontOffset    =    front.anchoredPosition.y;
      initialMidOffset      =      mid.anchoredPosition.y;
      initialBackOffset     =     back.anchoredPosition.y;

      x1 = initialFrontTopOffset - initialFrontOffset;
      x2 = initialMidTopOffset   - initialMidOffset;
      x3 = initialBackTopOffset  - initialBackOffset;
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

      _bgImageBackTop.material.SetVector("_Offset",  backOffset);
      _bgImageMidTop.material.SetVector("_Offset",   midOffset);
      _bgImageFrontTop.material.SetVector("_Offset", frontOffset);

      _bgImageBack.material.SetVector("_Offset",  backOffset);
      _bgImageMid.material.SetVector("_Offset",   midOffset);
      _bgImageFront.material.SetVector("_Offset", frontOffset);
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
