namespace com.github.k_stand.ksanimatorclipboard.editor.Copying
{
    internal static class AnimatorCopyClipSetTypeExtensions
    {
        internal static bool IsInStateMachineCategory(this AnimatorCopyClipSet.AnimatorCopyClipSetType setType) =>
            setType is AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildState
                or AnimatorCopyClipSet.AnimatorCopyClipSetType.ChildStateMachine
                or AnimatorCopyClipSet.AnimatorCopyClipSetType.Transition
                or AnimatorCopyClipSet.AnimatorCopyClipSetType.StateTransition
                or AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects;
    }
}
