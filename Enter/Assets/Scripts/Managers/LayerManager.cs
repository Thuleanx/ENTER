using UnityEngine;

namespace Enter
{
  [DisallowMultipleComponent]
  public class LayerManager : MonoBehaviour
  {
    public static LayerManager Instance;

    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private LayerMask _rcBoxLayer;
    [SerializeField] private LayerMask _rcAreaLayer;

    [SerializeField] private LayerMask _physicsBoxLayer;
    [SerializeField] private LayerMask _physicsBeamLayer;

    [SerializeField] private LayerMask _conveyorBoxLayer;
    [SerializeField] private LayerMask _conveyorBeamLayer;

    public LayerMask PlayerLayer         => _playerLayer;
    public LayerMask StaticGroundLayer   => _groundLayer;
    public LayerMask MovingGroundLayer   => _physicsBoxLayer | _conveyorBoxLayer;
    public LayerMask RCBoxGroundLayer    => _rcBoxLayer;
    public LayerMask AllGroundLayer      => StaticGroundLayer | MovingGroundLayer | RCBoxGroundLayer;

    public LayerMask RCBoxLayer          => _rcBoxLayer;
    public LayerMask RCAreaLayer         => _rcAreaLayer;
    public LayerMask CuttableLayer       => _physicsBoxLayer;

    public LayerMask PhysicsBoxLayer     => _physicsBoxLayer;
    public LayerMask PhysicsBeamLayer    => _physicsBeamLayer;

    public LayerMask ConveyorBoxLayer    => _conveyorBoxLayer;
    public LayerMask ConveyorBeamLayer   => _conveyorBeamLayer;

    public LayerMask BlockLaserLayer     => AllGroundLayer;

    void Awake() { Instance = this; }

    public bool IsInLayerMask(LayerMask layer, GameObject obj) 
    {
      return (layer & (1 << obj.layer)) != 0;
    }
  }
}