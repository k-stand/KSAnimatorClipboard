using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    internal interface IStateMachineBehaviourCloneResultValidator
    {
        Type BehaviourType { get; }

        IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour);
    }
}
