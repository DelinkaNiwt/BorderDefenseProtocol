namespace BDP.Core.Expressions
{
    /// <summary>
    /// Verb 条目在表达系统中的正规化主副身份。
    /// </summary>
    public enum VerbAttackRole
    {
        /// <summary>
        /// 当前条目不是 Verb。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前条目是主攻击 Verb。
        /// </summary>
        Primary = 1,

        /// <summary>
        /// 当前条目是副攻击 Verb。
        /// </summary>
        Secondary = 2
    }
}
