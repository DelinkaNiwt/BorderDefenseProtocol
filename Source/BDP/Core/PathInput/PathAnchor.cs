using Verse;

namespace BDP.Core.PathInput
{
    /// <summary>
    /// 通用路径锚点 — 纯数据，描述玩家Shift+点击选择的一个路径中转点。
    /// 不依赖任何射击或能力语义，可被毒蛇、蚱蜢等任意消费层复用。
    /// </summary>
    public class PathAnchor : IExposable
    {
        /// <summary>锚点所在格子的 X 坐标。</summary>
        public int X { get; set; }

        /// <summary>锚点所在格子的 Z 坐标。</summary>
        public int Z { get; set; }

        /// <summary>从地图格创建锚点。</summary>
        public static PathAnchor FromCell(IntVec3 cell)
        {
            return new PathAnchor { X = cell.x, Z = cell.z };
        }

        /// <summary>将锚点转换回地图格。</summary>
        public IntVec3 ToCell()
        {
            return new IntVec3(X, 0, Z);
        }

        /// <summary>深度复制当前锚点。</summary>
        public PathAnchor CloneTyped()
        {
            return new PathAnchor { X = X, Z = Z };
        }

        /// <summary>存档序列化。</summary>
        public void ExposeData()
        {
            int x = X;
            int z = Z;
            Scribe_Values.Look(ref x, "x", 0);
            Scribe_Values.Look(ref z, "z", 0);
            X = x;
            Z = z;
        }

        /// <summary>调试用字符串表示。</summary>
        public override string ToString()
        {
            return $"({X}, {Z})";
        }
    }
}
