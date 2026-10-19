using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Information;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>复制和合并制造配置中的显式功能声明。</summary>
    public static class ChipFeatureMergeService
    {
        /// <summary>复制单个配置的功能声明和来源列表。</summary>
        public static List<ChipFeatureDeclaration> CloneFeatures(
            IList<ChipFeatureDeclaration> source,
            ChipFeatureCloneContext context,
            string prefix = null)
        {
            List<ChipFeatureDeclaration> result =
                new List<ChipFeatureDeclaration>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipFeatureDeclaration declaration = source[index];
                if (declaration != null)
                {
                    result.Add(CloneDeclaration(declaration, context, prefix));
                }
            }

            return result;
        }

        /// <summary>按制造顺序复制两个独立功能实例集合。</summary>
        public static List<ChipFeatureDeclaration> MergeDual(
            ChipManufacturingPresetDef first,
            ChipManufacturingPresetDef second,
            ChipFeatureCloneContext context)
        {
            List<ChipFeatureDeclaration> result = CloneFeatures(
                first?.config?.Features,
                context,
                "mfg_0");
            result.AddRange(CloneFeatures(
                second?.config?.Features,
                context,
                "mfg_1"));
            return result;
        }

        /// <summary>递归复制功能实例、来源和子功能。</summary>
        private static ChipFeatureDeclaration CloneDeclaration(
            ChipFeatureDeclaration source,
            ChipFeatureCloneContext context,
            string prefix)
        {
            string cloneId = Prefix(prefix, source.Id);
            context?.RegisterFeature(Prefix(prefix, source.Id), cloneId);
            return new ChipFeatureDeclaration
            {
                Id = cloneId,
                Feature = source.Feature,
                LabelKey = source.LabelKey,
                Sources = CloneSources(source.Sources, context, prefix),
                Groups = CloneGroups(source.Groups, context, prefix),
                Subgroups = CloneSubgroups(source.Subgroups, context, prefix),
                Children = CloneFeatures(source.Children, context, prefix)
            };
        }

        /// <summary>递归复制功能信息组及其独立来源集合。</summary>
        private static List<ChipFeatureGroupDeclaration> CloneGroups(
            IList<ChipFeatureGroupDeclaration> source,
            ChipFeatureCloneContext context,
            string prefix)
        {
            List<ChipFeatureGroupDeclaration> result =
                new List<ChipFeatureGroupDeclaration>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipFeatureGroupDeclaration item = source[index];
                if (item == null)
                {
                    continue;
                }

                result.Add(new ChipFeatureGroupDeclaration
                {
                    Id = item.Id,
                    Group = item.Group,
                    LabelKey = item.LabelKey,
                    Sources = CloneSources(item.Sources, context, prefix),
                    Subgroups = CloneSubgroups(item.Subgroups, context, prefix)
                });
            }

            return result;
        }

        /// <summary>复制信息组的一层子组及其独立来源集合。</summary>
        private static List<ChipFeatureSubgroupDeclaration> CloneSubgroups(
            IList<ChipFeatureSubgroupDeclaration> source,
            ChipFeatureCloneContext context,
            string prefix)
        {
            List<ChipFeatureSubgroupDeclaration> result =
                new List<ChipFeatureSubgroupDeclaration>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipFeatureSubgroupDeclaration item = source[index];
                if (item == null)
                {
                    continue;
                }

                result.Add(new ChipFeatureSubgroupDeclaration
                {
                    Id = item.Id,
                    Group = item.Group,
                    LabelKey = item.LabelKey,
                    Sources = CloneSources(item.Sources, context, prefix)
                });
            }

            return result;
        }

        /// <summary>复制功能来源并同步其条目引用。</summary>
        private static List<ChipFeatureSourceConfig> CloneSources(
            IList<ChipFeatureSourceConfig> source,
            ChipFeatureCloneContext context,
            string prefix)
        {
            List<ChipFeatureSourceConfig> result =
                new List<ChipFeatureSourceConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipFeatureSourceConfig item = source[index];
                if (item == null)
                {
                    continue;
                }

                string clonedSourceId = Prefix(prefix, item.Id);
                ChipFeatureSourceConfig clone = item.CloneForRemap(
                    clonedSourceId,
                    entryId => ResolveEntry(
                        context,
                        Prefix(prefix, entryId),
                        prefix));

                context?.RegisterSource(Prefix(prefix, item.Id), clone.Id);
                result.Add(clone);
            }

            return result;
        }

        /// <summary>给制造双预设 Id 加稳定前缀；单预设保留原 Id。</summary>
        private static string Prefix(string prefix, string value)
        {
            return string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(prefix)
                ? value
                : prefix + "_" + value;
        }

        /// <summary>有上下文时读取映射，没有上下文时保留复制后的 Id。</summary>
        private static string ResolveEntry(
            ChipFeatureCloneContext context,
            string entryId,
            string prefix)
        {
            if (context == null)
            {
                return Prefix(prefix, entryId);
            }

            return context.ResolveEntry(entryId);
        }
    }
}
