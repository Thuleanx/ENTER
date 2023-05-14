using UnityEngine;
using NaughtyAttributes;

namespace Enter
{
  // Contains some properties that can decide the level's behaviour
  public class SpawnPoint : MonoBehaviour
  {
    [SerializeField, OnValueChanged("onChange")] private bool _canCutPaste   = false;
    [SerializeField, OnValueChanged("onChange")] private bool _canRCAnywhere = false;
    [SerializeField, OnValueChanged("onChange")] private bool _canDelete     = false;

    public bool CanCutPaste   => _canCutPaste;
    public bool CanRCAnywhere => _canRCAnywhere;
    public bool CanDelete     => _canDelete;

    private void onChange()
    {
      SceneTransitioner.Instance.UpdateRCBoxPermissionsFromCurrSpawnPoint();
    }
  }
}