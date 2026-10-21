using System;

namespace SquareTD
{
    public enum EnemyCubeFace { Front = 0, Back = 1, Left = 2, Right = 3, Top = 4, Bottom = 5 }
    public enum EnemyCubeFaceAsset { SelfHeal = 0, Defense = 1, Action = 2, Mobility = 3, Locked = 4, Hourglass = 5 }
    public enum EnemyCubeRollDirection { Right, Left, Up, Down }

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

        public static EnemyCubeOrientation FromFace(EnemyCubeFace face, int counterClockwiseQuarterTurns)
        {
            EnemyCubeOrientation orientation = Identity;
            switch (face)
            {
                case EnemyCubeFace.Front: break;
                case EnemyCubeFace.Back: orientation = orientation.Rolled(EnemyCubeRollDirection.Right).Rolled(EnemyCubeRollDirection.Right); break;
                case EnemyCubeFace.Left: orientation = orientation.Rolled(EnemyCubeRollDirection.Right); break;
                case EnemyCubeFace.Right: orientation = orientation.Rolled(EnemyCubeRollDirection.Left); break;
                case EnemyCubeFace.Top: orientation = orientation.Rolled(EnemyCubeRollDirection.Down); break;
                case EnemyCubeFace.Bottom: orientation = orientation.Rolled(EnemyCubeRollDirection.Up); break;
                default: throw new ArgumentOutOfRangeException(nameof(face));
            }
            int turns = ((counterClockwiseQuarterTurns % 4) + 4 - orientation.CounterClockwiseQuarterTurns) % 4;
            for (int i = 0; i < turns; i++)
            {
                orientation.m_right = RotateCounterClockwise(orientation.m_right);
                orientation.m_up = RotateCounterClockwise(orientation.m_up);
                orientation.m_front = RotateCounterClockwise(orientation.m_front);
            }
            return orientation;
        }

        private static Axis RotateCounterClockwise(Axis axis) => new Axis(-axis.y, axis.x, axis.z);

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

        public int CounterClockwiseQuarterTurns
        {
            get
            {
                EnemyCubeFace face = VisibleFace;
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
}
