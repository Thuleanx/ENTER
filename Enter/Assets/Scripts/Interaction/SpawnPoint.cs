using UnityEngine;

namespace Enter
{
  // Contains some properties that can decide the level's behaviour
  public class SpawnPoint : MonoBehaviour
  {
    [SerializeField] private bool _canCutPaste   = false;
    [SerializeField] private bool _canRCAnywhere = false;
    [SerializeField] private bool _canDelete     = false;

    public bool CanCutPaste   => _canCutPaste;
    public bool CanRCAnywhere => _canRCAnywhere;
    public bool CanDelete     => _canDelete;
  }
}