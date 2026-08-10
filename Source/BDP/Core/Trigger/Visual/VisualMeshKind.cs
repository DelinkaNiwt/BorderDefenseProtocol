namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 视觉姿态解析后选择的平面网格种类。
    /// 它显式表达 mesh 镜像，而不是在绘制处重新猜测。
    /// </summary>
    internal enum VisualMeshKind
    {
        /// <summary>
        /// 使用普通 plane10 网格。
        /// </summary>
        Plane,

        /// <summary>
        /// 使用 plane10Flip 水平镜像网格。
        /// </summary>
        PlaneFlipped
    }
}
