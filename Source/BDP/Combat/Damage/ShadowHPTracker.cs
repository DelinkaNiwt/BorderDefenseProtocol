using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 影子HP追踪器。
    /// 管理所有部位的影子HP，使用 BodyPartRecord.Index 作为key（唯一索引，天然区分左右和同名部位）。
    ///
    /// 职责：
    /// - 从快照初始化影子HP
    /// - 应用伤害到影子HP
    /// - 查询部位影子HP
    /// - 检测部位是否破坏
    /// </summary>
    public class ShadowHPTracker : IExposable
    {
        // key: BodyPartRecord.Index（每个部位在身体定义中的唯一整数索引）
        // value: 当前影子HP
        private Dictionary<int, float> shadowHP;

        /// <summary>
        /// 从快照初始化影子HP。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        public void InitializeFromSnapshot(Pawn pawn)
        {
            shadowHP = new Dictionary<int, float>();

            if (pawn?.health?.hediffSet == null)
                return;

            Log.Message($"[BDP] ═══════════════════════════════════════════════════");
            Log.Message($"[BDP] ShadowHPTracker 初始化开始: {pawn.LabelShort}");
            Log.Message($"[BDP] ═══════════════════════════════════════════════════");

            // 获取所有部位（包括缺失的）
            var allParts = pawn.RaceProps.body.AllParts;
            var notMissingParts = pawn.health.hediffSet.GetNotMissingParts().ToList();
            var missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();

            Log.Message($"[BDP] 部位统计:");
            Log.Message($"[BDP]   总部位数: {allParts.Count}");
            Log.Message($"[BDP]   存在部位: {notMissingParts.Count}");
            Log.Message($"[BDP]   缺失部位: {(missingParts != null ? missingParts.Count : 0)}");
            Log.Message($"");

            // 遍历所有部位，输出详细信息
            Log.Message($"[BDP] 部位详细信息:");
            Log.Message($"[BDP] {"Index",-6} {"部位标签",-20} {"defName",-15} {"状态",-8} {"当前HP",-10} {"最大HP",-10}");
            Log.Message($"[BDP] {new string('-', 75)}");

            foreach (var part in allParts)
            {
                int key = part.Index;
                bool isMissing = !notMissingParts.Contains(part);
                string status = isMissing ? "缺失" : "存在";

                if (!isMissing)
                {
                    // 存在的部位：初始化影子HP
                    float currentHP = pawn.health.hediffSet.GetPartHealth(part);
                    float maxHP = part.def.hitPoints;
                    shadowHP[key] = currentHP;

                    Log.Message($"[BDP] {key,-6} {part.Label,-20} {part.def.defName,-15} {status,-8} {currentHP,10:F1} {maxHP,10:F1}");
                }
                else
                {
                    // 缺失的部位：不初始化影子HP
                    float maxHP = part.def.hitPoints;
                    Log.Message($"[BDP] {key,-6} {part.Label,-20} {part.def.defName,-15} {status,-8} {"N/A",10} {maxHP,10:F1}");
                }
            }

            Log.Message($"[BDP] ═══════════════════════════════════════════════════");
            Log.Message($"[BDP] ShadowHPTracker 初始化完成: {shadowHP.Count} 个部位已加载");
            Log.Message($"[BDP] ═══════════════════════════════════════════════════");
        }

        /// <summary>
        /// 应用伤害到指定部位的影子HP。
        /// </summary>
        /// <param name="part">目标部位</param>
        /// <param name="damage">伤害量</param>
        public void TakeDamage(BodyPartRecord part, float damage)
        {
            if (part == null || damage <= 0f)
                return;

            int key = part.Index;
            if (!shadowHP.ContainsKey(key))
            {
                Log.Warning($"[BDP] ShadowHPTracker: 部位 {part.Label}(Index={key}) 不存在于影子HP表中");
                return;
            }

            shadowHP[key] = UnityEngine.Mathf.Max(0f, shadowHP[key] - damage);
        }

        /// <summary>
        /// 获取指定部位的影子HP。
        /// </summary>
        /// <param name="part">目标部位</param>
        /// <returns>影子HP值，如果部位不存在返回0</returns>
        public float GetHP(BodyPartRecord part)
        {
            if (part == null)
                return 0f;

            int key = part.Index;
            return shadowHP.ContainsKey(key) ? shadowHP[key] : 0f;
        }

        /// <summary>
        /// 检查指定部位是否已破坏（影子HP≤0）。
        /// </summary>
        /// <param name="part">目标部位</param>
        /// <returns>true=已破坏，false=未破坏</returns>
        public bool IsDestroyed(BodyPartRecord part)
        {
            return GetHP(part) <= 0f;
        }

        /// <summary>
        /// 获取部位的影子HP百分比。
        /// </summary>
        /// <param name="part">目标部位</param>
        /// <returns>健康百分比（0.0 ~ 1.0），如果部位不存在返回0</returns>
        public float GetHealthPercentage(BodyPartRecord part)
        {
            if (part == null) return 0f;

            int key = part.Index;
            if (!shadowHP.ContainsKey(key))
                return 0f;

            float currentHP = shadowHP[key];
            float maxHP = part.def.hitPoints;

            if (maxHP <= 0f) return 0f;

            return currentHP / maxHP;
        }

        /// <summary>
        /// 清理影子HP数据。
        /// </summary>
        public void Clear()
        {
            shadowHP?.Clear();
        }

        /// <summary>
        /// 序列化影子HP数据。
        /// </summary>
        public void ExposeData()
        {
            // 序列化字典：将字典转换为两个列表
            List<int> keys = null;
            List<float> values = null;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (shadowHP != null)
                {
                    keys = new List<int>(shadowHP.Keys);
                    values = new List<float>(shadowHP.Values);
                }
            }

            Scribe_Collections.Look(ref keys, "shadowHP_keys", LookMode.Value);
            Scribe_Collections.Look(ref values, "shadowHP_values", LookMode.Value);

            // 读档时重建字典
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                shadowHP = new Dictionary<int, float>();
                if (keys != null && values != null && keys.Count == values.Count)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        shadowHP[keys[i]] = values[i];
                    }
                    Log.Message($"[BDP] ShadowHPTracker读档完成: {shadowHP.Count}个部位");
                }
            }
        }
    }
}
