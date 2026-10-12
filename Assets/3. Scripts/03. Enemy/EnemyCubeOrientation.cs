using System;

public enum EnemyCubeFace { Front, Back, Left, Right, Top, Bottom }
public enum EnemyCubeRollDirection { Right, Left, Up, Down }

/// <summary>
/// 큐브의 세 축을 정수로 보관합니다. 경로가 꺾여도 실제 90도 굴림의 자세를 이어 갑니다.
/// 부동소수점 회전 누적이나 이동 방향만으로 그림을 선택하지 않습니다.
/// </summary>
public struct EnemyCubeOrientation
{
    private struct Axis
    {
        public int x, y, z;
        public Axis(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    private Axis m_right, m_up, m_front;

    public static EnemyCubeOrientation Identity => new EnemyCubeOrientation
    {
        m_right = new Axis(1, 0, 0), m_up = new Axis(0, 1, 0), m_front = new Axis(0, 0, 1)
    };

    public EnemyCubeFace VisibleFace
    {
        get
        {
            if (m_front.z == 1) return EnemyCubeFace.Front;
            if (m_front.z == -1) return EnemyCubeFace.Back;
            if (m_right.z == 1) return EnemyCubeFace.Right;
            if (m_right.z == -1) return EnemyCubeFace.Left;
            return m_up.z == 1 ? EnemyCubeFace.Top : EnemyCubeFace.Bottom;
        }
    }

    // 스프라이트 윗변이 가리키는 방향: 0=위, 1=왼쪽, 2=아래, 3=오른쪽.
    public int CounterClockwiseQuarterTurns
    {
        get
        {
            EnemyCubeFace face = VisibleFace;
            // 위/아래 그림은 같은 머리 방향을 기준으로 배치합니다.
            Axis imageUp = face == EnemyCubeFace.Top || face == EnemyCubeFace.Bottom
                ? new Axis(-m_front.x, -m_front.y, -m_front.z) : m_up;
            if (imageUp.x == -1) return 1;
            if (imageUp.y == -1) return 2;
            if (imageUp.x == 1) return 3;
            return 0;
        }
    }

    public EnemyCubeOrientation Rolled(EnemyCubeRollDirection direction)
    {
        return new EnemyCubeOrientation
        {
            m_right = RotateAxis(m_right, direction),
            m_up = RotateAxis(m_up, direction),
            m_front = RotateAxis(m_front, direction)
        };
    }

    private static Axis RotateAxis(Axis axis, EnemyCubeRollDirection direction)
    {
        switch (direction)
        {
            case EnemyCubeRollDirection.Right: return new Axis(axis.z, axis.y, -axis.x);
            case EnemyCubeRollDirection.Left: return new Axis(-axis.z, axis.y, axis.x);
            case EnemyCubeRollDirection.Up: return new Axis(axis.x, axis.z, -axis.y);
            case EnemyCubeRollDirection.Down: return new Axis(axis.x, -axis.z, axis.y);
            default: throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }
}
