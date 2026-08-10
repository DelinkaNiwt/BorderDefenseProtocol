using System;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// BDP 攻击会话的唯一运行时身份令牌。
    /// 它把继续执行同一条攻击会话所需的最小真值收口成一个对象。
    /// </summary>
    internal sealed class AttackSessionToken : IExposable, IEquatable<AttackSessionToken>
    {
        /// <summary>
        /// 当前攻击会话所属的攻击实例标识。
        /// 它服务日志追踪与同轮执行串联，可在建单后补齐。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前攻击会话绑定的正式结果标识。
        /// 它回答“当前这轮会话到底承接哪条正式结果”。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前攻击会话命中的已发布投影版本号。
        /// 它用于阻止旧会话跨发布版本继续消费过期真值。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前攻击会话所属宿主 Pawn 的 ThingID。
        /// 它用于确保令牌不会跨 Pawn 误复用。
        /// </summary>
        public string OwnerPawnThingId { get; set; }

        /// <summary>
        /// 判断当前令牌是否具备继续执行所需的最小身份。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ResultId)
                    && ProjectionVersion > 0
                    && !string.IsNullOrWhiteSpace(OwnerPawnThingId);
            }
        }

        /// <summary>
        /// 为指定 Pawn 与已发布结果创建一份最小会话令牌。
        /// </summary>
        internal static AttackSessionToken Create(
            Pawn pawn,
            string resultId,
            int projectionVersion,
            string attackInstanceId = null)
        {
            return new AttackSessionToken
            {
                AttackInstanceId = attackInstanceId,
                ResultId = resultId,
                ProjectionVersion = projectionVersion,
                OwnerPawnThingId = pawn != null ? pawn.ThingID : null
            };
        }

        /// <summary>
        /// 判断当前令牌是否属于指定 Pawn。
        /// </summary>
        internal bool BelongsTo(Pawn pawn)
        {
            return pawn != null
                && !string.IsNullOrWhiteSpace(pawn.ThingID)
                && OwnerPawnThingId == pawn.ThingID;
        }

        /// <summary>
        /// 复制一份当前令牌。
        /// 后续任何按需补齐字段的操作都返回副本，不原地改共享对象。
        /// </summary>
        internal AttackSessionToken Clone()
        {
            return new AttackSessionToken
            {
                AttackInstanceId = AttackInstanceId,
                ResultId = ResultId,
                ProjectionVersion = ProjectionVersion,
                OwnerPawnThingId = OwnerPawnThingId
            };
        }

        /// <summary>
        /// 基于当前身份返回一份补齐了攻击实例标识的令牌副本。
        /// </summary>
        internal AttackSessionToken WithAttackInstanceId(string attackInstanceId)
        {
            AttackSessionToken clone = Clone();
            clone.AttackInstanceId = attackInstanceId;
            return clone;
        }

        /// <summary>
        /// 基于当前身份返回一份重绑到新发布版本的令牌副本。
        /// </summary>
        internal AttackSessionToken WithProjectionVersion(int projectionVersion)
        {
            AttackSessionToken clone = Clone();
            clone.ProjectionVersion = projectionVersion;
            return clone;
        }

        /// <summary>
        /// 判断当前令牌与另一份令牌是否完全相等。
        /// </summary>
        public bool Equals(AttackSessionToken other)
        {
            return !(other is null)
                && string.Equals(AttackInstanceId, other.AttackInstanceId, StringComparison.Ordinal)
                && string.Equals(ResultId, other.ResultId, StringComparison.Ordinal)
                && ProjectionVersion == other.ProjectionVersion
                && string.Equals(OwnerPawnThingId, other.OwnerPawnThingId, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断当前对象是否与另一对象表示同一份会话令牌。
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as AttackSessionToken);
        }

        /// <summary>
        /// 生成当前令牌的哈希值。
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (AttackInstanceId != null ? AttackInstanceId.GetHashCode() : 0);
                hash = hash * 31 + (ResultId != null ? ResultId.GetHashCode() : 0);
                hash = hash * 31 + ProjectionVersion;
                hash = hash * 31 + (OwnerPawnThingId != null ? OwnerPawnThingId.GetHashCode() : 0);
                return hash;
            }
        }

        /// <summary>
        /// 输出诊断友好的简短文本。
        /// 它只服务日志，不参与身份判断。
        /// </summary>
        public override string ToString()
        {
            return "AttackSessionToken("
                + "attackId=" + (AttackInstanceId ?? "null")
                + ", resultId=" + (ResultId ?? "null")
                + ", projection=" + ProjectionVersion
                + ", owner=" + (OwnerPawnThingId ?? "null")
                + ")";
        }

        /// <summary>
        /// 序列化当前令牌。
        /// 它直接进入 formal host 与 job 的存档链路。
        /// </summary>
        public void ExposeData()
        {
            string attackInstanceId = AttackInstanceId;
            string resultId = ResultId;
            int projectionVersion = ProjectionVersion;
            string ownerPawnThingId = OwnerPawnThingId;
            Scribe_Values.Look(ref attackInstanceId, "attackInstanceId");
            Scribe_Values.Look(ref resultId, "resultId");
            Scribe_Values.Look(ref projectionVersion, "projectionVersion", 0);
            Scribe_Values.Look(ref ownerPawnThingId, "ownerPawnThingId");
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            ProjectionVersion = projectionVersion;
            OwnerPawnThingId = ownerPawnThingId;
        }
    }
}
