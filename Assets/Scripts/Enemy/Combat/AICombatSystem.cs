using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UGG.Combat;
using UnityEngine.Rendering.PostProcessing;

public class AICombatSystem : CharacterCombatSystemBase
{
    [SerializeField, Header("检测范围中心")] private Transform detectionCenter;
    [SerializeField, Header("检测范围")] private float detectionRang;

    [SerializeField, Header("检测图层：敌人")] private LayerMask whatisEnemy;
    [SerializeField, Header("检测图层：障碍物")] private LayerMask whatisObs;

    private Collider[] colliderTargets = new Collider[1];
    private Collider[] detectionedTarget = new Collider[1];

    [SerializeField, Header("目标")] private Transform currentTarget;

    //AnimationID
    private int lockOnID = Animator.StringToHash("LockOn");

    [SerializeField] private float animationMoveMult;

    [SerializeField, Header("技能搭配")] private List<CombatSkillBase> skills = new List<CombatSkillBase>();

    private void Start()
    {
        InitAllSkill();
    }

    private void Update()
    {
        AIView();
        LockOnTarget();
        UpdateAnimationMove();
        DetectionTarget();
    }

    private void LateUpdate()
    {
        OnAnimatorActionAutoLockON();
    }

    /// <summary>
    /// AI视野
    /// </summary>
    private void AIView()
    {
        //检测球体内是否有敌人进入 有的话返回
        int targetCount = Physics.OverlapSphereNonAlloc(detectionCenter.position, detectionRang, colliderTargets, whatisEnemy);

        //如果目标数量大于0
        if (targetCount > 0)
        {
            //射线是否检测到障碍物
            if (!Physics.Raycast((transform.root.position + transform.root.up * 0.5f), (colliderTargets[0].transform.position - transform.root.position).normalized, out var hit, detectionRang, whatisObs))
            {
                //如果玩家和AI的角度大于0.15
                if (Vector3.Dot((colliderTargets[0].transform.position - transform.root.position).normalized, transform.root.forward) > 0.35f)
                {
                    //赋值
                    currentTarget = colliderTargets[0].transform;
                }
            }
        }
    }

    private void LockOnTarget()
    {
        //检测AI动画是否在Motion状态并且当前目标不为空
        if (_animator.CheckAnimationTag("Motion") && currentTarget != null)
        {
            _animator.SetFloat(lockOnID, 1f);
            transform.root.rotation = transform.LockOnTarget(currentTarget, transform, 50f);
        }
        else
        {
            _animator.SetFloat(lockOnID, 0f);
        }
    }

    public Transform GetCurrentTarget()
    {
        if(currentTarget == null)
        {
            return null;
        }

        return currentTarget;
    }

    private void UpdateAnimationMove()
    {
        if (_animator.CheckAnimationTag("Roll"))
        {
            _characterMovementBase.CharacterMoveInterface(transform.root.forward, _animator.GetFloat(animationMoveID) * animationMoveMult, true);
        }

        if (_animator.CheckAnimationTag("Attack"))
        {
            _characterMovementBase.CharacterMoveInterface(transform.root.forward, _animator.GetFloat(animationMoveID) * animationMoveMult, true);
        }
    }

    private void OnAnimatorActionAutoLockON()
    {
        //检测攻击状态是否允许自动锁定敌人
        if (CanAttackLockOn())
        {
            //检测动画是否在默认攻击状态或者大剑攻击状态 如果是的话执行看向目标位置方法
            if (_animator.CheckAnimationTag("Attack") || _animator.CheckAnimationTag("GSAttack"))
            {
                transform.root.rotation = transform.LockOnTarget(currentTarget, transform.root.transform, 50f);
            }
        }
    }

    #region 动作检测

    private bool CanAttackLockOn()
    {
        if (_animator.CheckAnimationTag("Attack") || _animator.CheckAnimationTag("GSAttack"))
        {
            if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.75f)
            {
                return true;
            }
        }
        return false;
    }

    private void DetectionTarget()
    {
        //检测球体范围内的目标
        int targetCount = Physics.OverlapSphereNonAlloc(detectionCenter.position, detectionRang, detectionedTarget, enemyLayer);

        //后续功能补充
        if (targetCount > 0)
        {
            SetCurrentTarget(detectionedTarget[0].transform);
        }
    }

    private void SetCurrentTarget(Transform target)
    {
        //如果当前目标等于空或者不等于当前传递进来的目标
        if (currentTarget == null || currentTarget != target)
        {
            //给当前目标赋值
            currentTarget = target;
        }
    }

    #endregion

    #region 技能

    private void InitAllSkill()
    {
        if (skills.Count == 0) return;

        for (int i = 0; i < skills.Count; i++)
        {
            skills[i].InitSkill(_animator, this, _characterMovementBase);

            //如果当前技能不允许使用
            if (!skills[i].GetSkillIsDone())
            {
                //重置
                skills[i].ResetSkill();
            }
        }
    }

    public CombatSkillBase GetAnDoneSkill()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].GetSkillIsDone()) return skills[i];
            else continue;
        }

        return null;
    }

    public CombatSkillBase GetSkillUseName(string name)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].GetSkillName().Equals(name)) return skills[i];
            else continue;
        }

        return null;
    }

    public CombatSkillBase GetSkillUseID(int id)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].GetSkillID() == id) return skills[i];
            else continue;
        }

        return null;
    }

    #endregion

    //获取当前目标与AI自身的距离
    public float GetCurrentTargetDistance() => Vector3.Distance(currentTarget.position, transform.root.position);

    //获取当前目标与AI自身的方向
    public Vector3 GetDirectionForTarget() => (currentTarget.position - transform.root.position).normalized;
}
