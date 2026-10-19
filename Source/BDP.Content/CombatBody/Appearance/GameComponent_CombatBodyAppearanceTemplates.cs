using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 存档级战斗体个人服装模板账本。
    /// </summary>
    public sealed class GameComponent_CombatBodyAppearanceTemplates : GameComponent
    {
        /// <summary>
        /// 当前存档中按 Pawn 归属保存的个人档案集合。
        /// </summary>
        private List<CombatBodyAppearanceProfile> profiles =
            new List<CombatBodyAppearanceProfile>();

        /// <summary>
        /// 供原版 GameComponent 自动构造。
        /// </summary>
        public GameComponent_CombatBodyAppearanceTemplates(Game game)
        {
        }

        /// <summary>
        /// 只读遍历当前存档的个人档案。
        /// </summary>
        public IEnumerable<CombatBodyAppearanceProfile> Profiles
        {
            get { return profiles; }
        }

        /// <summary>
        /// 查找指定 Pawn 的既有档案。
        /// </summary>
        public bool TryGetProfile(Pawn pawn, out CombatBodyAppearanceProfile profile)
        {
            profile = null;
            if (pawn == null || profiles == null)
            {
                return false;
            }

            for (int index = 0; index < profiles.Count; index++)
            {
                CombatBodyAppearanceProfile candidate = profiles[index];
                if (candidate != null && candidate.Matches(pawn))
                {
                    candidate.BindPawn(pawn);
                    profile = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 查找或创建指定 Pawn 的默认镜像档案。
        /// </summary>
        public CombatBodyAppearanceProfile GetOrCreateProfile(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (TryGetProfile(pawn, out CombatBodyAppearanceProfile profile))
            {
                return profile;
            }

            profile = new CombatBodyAppearanceProfile(pawn);
            profiles.Add(profile);
            return profile;
        }

        /// <summary>
        /// 移除指定 Pawn 的完整个人档案。
        /// </summary>
        public bool RemoveProfile(Pawn pawn)
        {
            if (pawn == null || profiles == null)
            {
                return false;
            }

            bool removed = false;
            for (int index = profiles.Count - 1; index >= 0; index--)
            {
                CombatBodyAppearanceProfile profile = profiles[index];
                if (profile != null && profile.Matches(pawn))
                {
                    profiles.RemoveAt(index);
                    removed = true;
                }
            }

            return removed;
        }

        /// <summary>
        /// 保存或读取当前存档的个人档案集合。
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref profiles, "combatBodyAppearanceProfiles", LookMode.Deep);
            if (profiles == null)
            {
                profiles = new List<CombatBodyAppearanceProfile>();
            }
        }
    }
}
