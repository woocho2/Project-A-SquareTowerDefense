namespace SquareTD
{
    // 차지는 무조건 자동 소모 (다 차면 다음 스킬 2에 바로 씀). 스킬이 받던 자리만 남겨 둠
    public class StackModeSettings
    {
        public bool ShouldUseStack(TowerDefinition definition, TowerStack stack) => stack.IsFull;
    }
}
