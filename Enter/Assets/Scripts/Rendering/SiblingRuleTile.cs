using UnityEngine;
using UnityEngine.Tilemaps;

namespace Enter {
    [CreateAssetMenu]
    public class SiblingRuleTile : RuleTile {
        public enum SiblingGroup {
            Ground,
            Wires
        }
        public SiblingGroup siblingGroup;

        static bool matchGroup(SiblingGroup us, SiblingGroup other) {
            if (us == other) return true;
            if (us == SiblingGroup.Ground) return other == SiblingGroup.Wires;
            return false;
        }

        public override bool RuleMatch(int neighbor, TileBase other) {
            // we dont really care for this
            if (other is RuleOverrideTile)
                other = (other as RuleOverrideTile).m_InstanceTile;

            switch (neighbor)
            {
                case TilingRule.Neighbor.This:
                    {
                        return other is SiblingRuleTile
                            && matchGroup(this.siblingGroup, (other as SiblingRuleTile).siblingGroup);
                    }
                case TilingRule.Neighbor.NotThis:
                    {
                        return !(other is SiblingRuleTile
                                && matchGroup(this.siblingGroup, (other as SiblingRuleTile).siblingGroup));
                    }
            }

            return base.RuleMatch(neighbor, other);
        }
    }
}
