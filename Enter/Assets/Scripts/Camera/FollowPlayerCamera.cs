using UnityEngine;
using Cinemachine;

namespace Enter
{
  [RequireComponent(typeof(CinemachineVirtualCamera))]
  public class FollowPlayerCamera : MonoBehaviour
  {
    private CinemachineVirtualCamera     _cam;
    private CinemachineFramingTransposer _cft;

    private float _defaultDeadZoneHeight;

    void Awake()
    {
      _cam = GetComponent<CinemachineVirtualCamera>();
      _cft = _cam.GetCinemachineComponent<CinemachineFramingTransposer>();

      _defaultDeadZoneHeight = _cft.m_DeadZoneHeight;
    }

    void Update()
    {
      if (!_cam.Follow) _cam.Follow = PlayerManager.Player.transform;
      
      // Avoid vertical virtual camera movement when ungrounded, by increasing dead-zone height
      _cft.m_DeadZoneHeight = PlayerManager.PlayerGrounded ? _defaultDeadZoneHeight : _cft.m_SoftZoneHeight;
    }
  }
}
