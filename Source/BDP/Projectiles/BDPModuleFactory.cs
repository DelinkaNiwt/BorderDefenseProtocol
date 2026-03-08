using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 模块工厂——根据ThingDef上的DefModExtension自动创建对应模块实例。
    ///
    /// 注册方式（在BDPMod静态构造函数中）：
    ///   BDPModuleFactory.Register&lt;BeamTrailConfig&gt;(cfg => new TrailModule(cfg));
    ///
    /// 创建流程：遍历def.modExtensions，匹配已注册的配置类型，调用工厂方法创建模块。
    /// </summary>
    public static class BDPModuleFactory
    {
        /// <summary>配置类型 → 工厂方法 的注册表。</summary>
        private static readonly Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>> registry
            = new Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>>();

        /// <summary>
        /// 注册一个配置类型对应的模块工厂。
        /// TConfig必须是DefModExtension子类，挂在ThingDef.modExtensions上。
        /// </summary>
        public static void Register<TConfig>(Func<TConfig, IBDPProjectileModule> factory)
            where TConfig : DefModExtension
        {
            registry[typeof(TConfig)] = ext => factory((TConfig)ext);
        }

        /// <summary>
        /// 根据ThingDef上的modExtensions创建所有匹配的模块实例。
        /// 返回的列表未排序，调用方需按Priority排序。
        /// </summary>
        public static List<IBDPProjectileModule> CreateModules(ThingDef def)
        {
            var modules = new List<IBDPProjectileModule>();
            if (def?.modExtensions == null) return modules;

            foreach (var ext in def.modExtensions)
            {
                if (ext == null) continue;
                Type extType = ext.GetType();
                if (registry.TryGetValue(extType, out var factory))
                {
                    try
                    {
                        var module = factory(ext);
                        if (module != null)
                            modules.Add(module);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[BDP] 模块创建失败: {extType.Name} → {e}");
                    }
                }
            }
            return modules;
        }
    }
}
