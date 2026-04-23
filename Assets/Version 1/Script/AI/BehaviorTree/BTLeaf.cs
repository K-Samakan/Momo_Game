using System;

namespace MOMO.AI.BehaviorTree
{
    // Action หรือ Condition แบบ lambda ใช้ง่ายไม่ต้องสร้างไฟล์ใหม่ทุกครั้ง
    public class BTLeaf : BTNode
    {
        readonly Func<BTStatus> _fn;
        public BTLeaf(Func<BTStatus> fn) => _fn = fn;
        public override BTStatus Tick() => _fn();
    }
}