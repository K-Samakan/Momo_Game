using System.Collections.Generic;

namespace MOMO.AI.BehaviorTree
{
    // ลอง node ทีละตัว ถ้าตัวไหน Success หรือ Running หยุดทันที
    // ถ้าทุกตัว Failure → return Failure
    public class BTSelector : BTNode
    {
        readonly List<BTNode> _children;
        public BTSelector(List<BTNode> children) => _children = children;

        public override BTStatus Tick()
        {
            foreach (var child in _children)
            {
                var s = child.Tick();
                if (s != BTStatus.Failure) return s;
            }
            return BTStatus.Failure;
        }
    }
}