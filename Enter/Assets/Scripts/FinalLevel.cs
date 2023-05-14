using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Enter
{
  public class FinalLevel : MonoBehaviour
  {
    public static FinalLevel Instance;

    private static int _checkpoint = 2;
    public static int Checkpoint
    {
      get
      {
        return _checkpoint;
      }
      private set
      {
        _checkpoint = value; 
        if (Instance != null) Instance.onCheckpointChanged();
      }
    }

    [SerializeField] private List<SpawnPoint> _checkpoints = new List<SpawnPoint>();

    [SerializeField] private GameObject _rcAreaGameObject;
    [SerializeField] private GameObject _extraVirusGameObject;
    [SerializeField] private GameObject _extraGroundGameObject;

    [SerializeField] private SpriteRenderer _bigWallSpriteRenderer;
    [SerializeField] private Color _yellowColor;
    [SerializeField] private Color _blackColor;

    [SerializeField] private Tilemap  _rcAreaTilemap;
    [SerializeField] private TileBase _rcAreaTileBase;

    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private Queue<Vector3Int> _boundaryTileIndices = new Queue<Vector3Int>();

    private int _count = 0;
    private int _expandCounts = 3;

    #region ================== Methods

    [Button] public void SetCheckpoint0() => Checkpoint = 0;
    [Button] public void SetCheckpoint1() => Checkpoint = 1;
    [Button] public void SetCheckpoint2() => Checkpoint = 2;
    [Button] public void SetCheckpoint3() => Checkpoint = 3;

    void Awake()
    {
      Instance = this; // Notice how this overwrites; its state is captured entirely by the checkpoint value
      onCheckpointChanged();
    }
    
    void Start()
    {
      Vector3Int tl = _rcAreaTilemap.layoutGrid.WorldToCell((Vector2) transform.position + new Vector2(-0.5f, 0.5f));

      _boundaryTileIndices.Enqueue(tl);
      _boundaryTileIndices.Enqueue(tl + new Vector3Int(1,  0));
      _boundaryTileIndices.Enqueue(tl + new Vector3Int(0, -1));
      _boundaryTileIndices.Enqueue(tl + new Vector3Int(1, -1));
    }

    void FixedUpdate()
    {
      if (Checkpoint == 1 && _count < _expandCounts + 2)
      {
        bool hit = Physics2D.OverlapCircle((Vector2) transform.position, 20, LayerManager.Instance.RCBoxLayer);
        if (hit) TriggerExpansion();
      }
    }

    public void TriggerExpansion()
    {
      float force = (_count + 1) / 4f;

      if (_count < _expandCounts)
      {
        _impulseSource.GenerateImpulseWithForce(force);
        RCBoxManager.Instance.DespawnRCBox();
        for (int i = 0; i < _count; i++) recursivelyAddRCAreaTiles();
      }
      else if (_count == _expandCounts + 1)
      {
        _impulseSource.GenerateImpulseWithForce(force);
        SetCheckpoint2();
      }
  
      _count++;
    }

    #endregion

    #region ================== Helpers

    private void enableAppropriateSpawnPoints(int i)
    {
      foreach (SpawnPoint x in FindObjectsOfType<SpawnPoint>())
        x.gameObject.SetActive(false);

      _checkpoints[i].gameObject.SetActive(true);
    }

    private void onCheckpointChanged()
    {
      enableAppropriateSpawnPoints(Checkpoint);
      SceneTransitioner.Instance.UpdateCurrSpawnPoint(_checkpoints[Checkpoint]);

      switch (Checkpoint)
      {
        case 0:
        {
          _rcAreaGameObject.SetActive(true);
          _extraVirusGameObject.SetActive(false);
          _extraGroundGameObject.SetActive(false);
          break;
        }
        case 1:
        {
          _rcAreaGameObject.SetActive(true);
          _extraVirusGameObject.SetActive(false);
          _extraGroundGameObject.SetActive(false);
          break;
        }
        case 2:
        {
          StartCoroutine(pulseYellow());
          _rcAreaGameObject.SetActive(false);
          _extraVirusGameObject.SetActive(true);
          _extraGroundGameObject.SetActive(true);
          break;
        }
      }
    }

    private void recursivelyAddRCAreaTiles()
    {
      Queue<Vector3Int> oldBoundary = _boundaryTileIndices;
      Queue<Vector3Int> newBoundary = new Queue<Vector3Int>();

      while (oldBoundary.Count > 0)
      {
        Vector3Int currentIndices = oldBoundary.Dequeue();

        bool up    = (_rcAreaTilemap.GetTile(currentIndices + Vector3Int.up)    != null);
        bool down  = (_rcAreaTilemap.GetTile(currentIndices + Vector3Int.down)  != null);
        bool left  = (_rcAreaTilemap.GetTile(currentIndices + Vector3Int.left)  != null);
        bool right = (_rcAreaTilemap.GetTile(currentIndices + Vector3Int.right) != null);

        if (!up)    newBoundary.Enqueue(currentIndices + Vector3Int.up);
        if (!down)  newBoundary.Enqueue(currentIndices + Vector3Int.down);
        if (!left)  newBoundary.Enqueue(currentIndices + Vector3Int.left);
        if (!right) newBoundary.Enqueue(currentIndices + Vector3Int.right);
      }

      foreach (Vector3Int indices in newBoundary)
      {
        _rcAreaTilemap.SetTile(indices, _rcAreaTileBase);
      }

      _boundaryTileIndices = newBoundary;
    }

    private IEnumerator pulseYellow()
    {
      Tween tween = _bigWallSpriteRenderer.DOColor(_yellowColor, 0.2f);
      tween.SetEase(Ease.InQuint).Play();

      yield return new WaitForSeconds(0.5f);

      tween = _bigWallSpriteRenderer.DOColor(_blackColor, 5f);
      tween.SetEase(Ease.OutCubic).Play();

      yield return new WaitForSeconds(5f);
    }

    #endregion
  }
}

