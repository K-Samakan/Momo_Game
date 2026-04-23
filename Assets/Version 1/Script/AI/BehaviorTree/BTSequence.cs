using System.Collections.Generic;

namespace MOMO.AI.BehaviorTree
{
    // รัน node ทีละตัว ถ้าตัวไหน Failure หยุดทันที
    // ทุกตัว Success → return Success
    public class BTSequence : BTNode
    {
        readonly List<BTNode> _children;
        public BTSequence(List<BTNode> children) => _children = children;

        public override BTStatus Tick()
        {
            foreach (var child in _children)
            {
                var s = child.Tick();
                if (s != BTStatus.Success) return s;
            }
            return BTStatus.Success;
        }
    }
}