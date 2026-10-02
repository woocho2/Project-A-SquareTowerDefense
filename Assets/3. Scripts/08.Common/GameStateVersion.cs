/// <summary>
/// 화면에 보이는 게임 상태(타워·적의 스탯, 배치, 재화, 턴 상태)가 바뀔 때마다 1씩 오르는 번호입니다.
/// 정보 패널은 매 프레임 글자를 다시 쓰는 대신, 이 번호가 달라졌을 때만 갱신합니다.
/// 상태를 바꾸는 쪽은 무엇이 바뀌었는지 알릴 필요 없이 MarkChanged()만 부르면 됩니다.
/// </summary>
public static class GameStateVersion
{
    public static int Current { get; private set; }

    public static void MarkChanged()
    {
        unchecked { Current++; }
    }
}
