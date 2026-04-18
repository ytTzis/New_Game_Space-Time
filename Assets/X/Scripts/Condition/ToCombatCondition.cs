using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToCombatCondition", menuName = "StateMachine/Condition/ToCombatCondition")]
public class ToCombatCondition : ConditionSO
{
    public override bool ConditionSetUp()
    {
        //�����ǰĿ�겻���ڿշ���true ���򷵻�false
        return (_combatSystem.GetCurrentTarget() != null) ? true : false;
    }
}
