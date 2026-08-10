using UnityEngine;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 原版 Targeter 当前这一小段点击调用链对应的输入事实快照。
    /// 它只承载运行时按钮与修饰键，不持有任何业务解释。
    /// </summary>
    public sealed class TargetingInputRuntimeFacts
    {
        /// <summary>
        /// 当前输入对应的按钮事实。
        /// </summary>
        public TargetingInputButton PressedButton { get; set; } = TargetingInputButton.None;

        /// <summary>
        /// 当前输入对应的修饰键事实。
        /// </summary>
        public TargetingInputModifiers Modifiers { get; set; } = TargetingInputModifiers.None;

        /// <summary>
        /// 从当前原版事件构建一份中性输入事实快照。
        /// 没有事件时返回空事实。
        /// </summary>
        public static TargetingInputRuntimeFacts FromEvent(Event currentEvent)
        {
            if (currentEvent == null)
            {
                return new TargetingInputRuntimeFacts();
            }

            return new TargetingInputRuntimeFacts
            {
                PressedButton = ResolveButton(currentEvent.button),
                Modifiers = ResolveModifiers(currentEvent)
            };
        }

        /// <summary>
        /// 把原版鼠标按钮编号映射到中性按钮枚举。
        /// </summary>
        private static TargetingInputButton ResolveButton(int button)
        {
            switch (button)
            {
                case 0:
                    return TargetingInputButton.Left;
                case 1:
                    return TargetingInputButton.Right;
                case 2:
                    return TargetingInputButton.Middle;
                default:
                    return TargetingInputButton.None;
            }
        }

        /// <summary>
        /// 从原版事件读取当前修饰键集合。
        /// </summary>
        private static TargetingInputModifiers ResolveModifiers(Event currentEvent)
        {
            TargetingInputModifiers modifiers = TargetingInputModifiers.None;
            if (currentEvent == null)
            {
                return modifiers;
            }

            if (currentEvent.shift)
            {
                modifiers |= TargetingInputModifiers.Shift;
            }

            if (currentEvent.control)
            {
                modifiers |= TargetingInputModifiers.Control;
            }

            if (currentEvent.alt)
            {
                modifiers |= TargetingInputModifiers.Alt;
            }

            return modifiers;
        }
    }
}
