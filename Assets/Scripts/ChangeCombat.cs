using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UGG.Combat;

public class ChangeCombat : StateMachineBehaviour
{
    private AICombatSystem _aiCombatSystem;

    [SerializeField] private float detectionTime;

    [SerializeField] private bool canChangeCombat;//当前是否允许变招
    [SerializeField] private bool allowReleaseChangeCombat;//是否允许释放变招技能

    [SerializeField] private string changeCombatName;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_aiCombatSystem == null)
        {
            _aiCombatSystem = animator.GetComponent<AICombatSystem>();
        }

        canChangeCombat = true;
        allowReleaseChangeCombat = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        canChangeCombat = false;
        allowReleaseChangeCombat = false;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CheckChangeCombatTime(animator);
        ChangeCombatAction(animator);
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}

    private void CheckChangeCombatTime(Animator animator)
    {
        if (_aiCombatSystem == null) return;
        if (_aiCombatSystem.GetCurrentTarget() == null) return;

        //如果当前动画状态时间小于指定时间 允许变招 大于则不允许
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < detectionTime)
        {
            canChangeCombat = true;
        }
        else if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > detectionTime)
        {
            canChangeCombat = false;
        }
    }

    private void ChangeCombatAction(Animator animator)
    {
        if (_aiCombatSystem == null) return;
        if (_aiCombatSystem.GetCurrentTarget() == null) return;

        //如果处于允许变招的时间段 就去检测玩家与自身的距离是否小于2.5f 如果小于就允许释放变招技能
        if (canChangeCombat)
        {
            if (_aiCombatSystem.GetCurrentTargetDistance() < 2.5f)
            {
                //allowReleaseChangeCombat = true;
                animator.CrossFade(changeCombatName, 0f, 0, 0f);
            }
        }

        //超过变招检测时间 但允许释放变招技能
        if (!canChangeCombat && allowReleaseChangeCombat)
        {
            //animator.CrossFade(changeCombatName, 0f, 0, 0f);
        }
    }
}
