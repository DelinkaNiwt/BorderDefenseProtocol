namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// LOS 检查模块配置
    /// 用于在 XML 中配置 LosCheckModule（未来扩展）
    /// </summary>
    public class LosCheckConfig : ShotModuleConfig
    {
        // 当前版本：配置类仅作为标记，实际模块在 ShotPipeline.Build() 中硬编码实例化
        // 未来版本：可添加 CreateModule() 方法支持 XML 驱动的模块构建
    }
}
