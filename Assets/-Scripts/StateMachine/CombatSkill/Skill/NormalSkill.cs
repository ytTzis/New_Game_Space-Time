using System.Collections;
using System.Collections.Generic;
using UGG.Move;
using UnityEngine;

[CreateAssetMenu(fileName = "NormalSkill", menuName = "Skill/NormalSkill")]
public class NormalSkill : CombatSkillBase
{
    public override void InvokeSkill()
    {
        if (animator.CheckAnimationTag("Motion") && skillIsDone)
        {
            //当技能被激活 但还没进入允许释放距离
            if (combat.GetCurrentTargetDistance() > skillUseDistance + 0.1f)
            {
                movement.CharacterMoveInterface(combat.GetDirectionForTarget(), 1.4f, true);

                animator.SetFloat(verticalID, 1f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);
                //animator.SetFloat(runID, 1f, 0.25f, Time.deltaTime);
            }
            else
            {
                UseSkill();
            }
        }
    }
}
