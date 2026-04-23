namespace MOMO.AI.BehaviorTree
{
    public enum BTStatus { Success, Failure, Running }

    public abstract class BTNode
    {
        public abstract BTStatus Tick();
    }
}