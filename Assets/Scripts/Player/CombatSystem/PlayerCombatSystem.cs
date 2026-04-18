using System;
using System.Collections;
using System.Collections.Generic;
using UGG.Health;
using UnityEngine;

namespace UGG.Combat
{
    public class PlayerCombatSystem : CharacterCombatSystemBase
    {
        private PlayerHealthSystem healthSystem;

        //引用
        [SerializeField] private Transform currentTarget;

        //Speed
        [SerializeField, Header("攻击移动速度倍率"), Range(0.1f, 10f)]
        private float attackMoveMult;
        
        //检测
        [SerializeField, Header("检测敌人")] private Transform detectionCenter;
        [SerializeField] private float detectionRang;

        //缓存
        private Collider[] detectionedTarget = new Collider[1];

        //允许攻击输入
        [SerializeField] private bool allowAttackInput;

        protected override void Awake()
        {
            base.Awake();

            healthSystem = GetComponentInParent<PlayerHealthSystem>();
        }

        private void Update()
        {
            PlayerAttackAction();
            DetectionTarget();
            ActionMotion();
            UpdateCurrentTarget();
            PlayerParryInput();
        }

        private void LateUpdate()
        {
            OnAnimatorActionAutoLockON();
        }

        private void PlayerAttackAction()
        {
            //当玩家处于Motion状态(idle)也允许玩家输入攻击信号
            if (!allowAttackInput)
            {
                if (_animator.CheckCurrentTagAnimationTimeIsExceed("Motion", 0.01f) && !_animator.IsInTransition(0))
                {
                    SetAllowAttackInput(true);
                }
            }

            //如果玩按下鼠标左键
            if (_characterInputSystem.playerLAtk && allowAttackInput)
            {
                if (healthSystem.GetCanExecute())
                {
                    //播放处决动画
                    _animator.Play("Execute_0", 0, 0f);

                    Time.timeScale = 1f;
                }
                else
                {
                    //触发默认攻击动画
                    _animator.SetTrigger(lAtkID);

                    SetAllowAttackInput(false);
                }
            }

            //如果玩家一直按住鼠标右键
            if (_characterInputSystem.playerRAtk)
            {
                //并且按下左键
                if (_characterInputSystem.playerLAtk)
                {
                    //触发大剑攻击动画
                    _animator.SetTrigger(lAtkID);

                    SetAllowAttackInput(false);
                }
            }

            _animator.SetBool(secondaryWeaponID, _characterInputSystem.playerRAtk);
        }

        private void PlayerParryInput()
        {
            if (CanInputParry())
            {
                _animator.SetBool(defenID, _characterInputSystem.playerDefen);
            }
            else
            {
                _animator.SetBool(defenID, false);
            }
        }

        private bool CanInputParry()
        {
            if (_animator.CheckAnimationTag("Motion")) return true;
            if (_animator.CheckAnimationTag("Parry")) return true;
            if (_animator.CheckCurrentTagAnimationTimeIsExceed("Hit", 0.07f)) return true;

            return false;
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

        private void ActionMotion()
        {
            if (_animator.CheckAnimationTag("Attack") || _animator.CheckAnimationTag("GSAttack"))
            {
                _characterMovementBase.CharacterMoveInterface(transform.forward, _animator.GetFloat(animationMoveID) * attackMoveMult, true);
            }
        }

        #region 动作检测
        
        /// <summary>
        /// 攻击状态是否允许自动锁定敌人
        /// </summary>
        /// <returns></returns>
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

        private void UpdateCurrentTarget()
        {
            if (_animator.CheckAnimationTag("Motion"))
            {
                if (_characterInputSystem.playerMovement.magnitude > 0)
                {
                    currentTarget = null;
                }
            }
        }

        /// <summary>
        /// 获取当前是否允许玩家攻击输入
        /// </summary>
        /// <returns></returns>
        public bool GetAllowAttackInput() => allowAttackInput;

        /// <summary>
        /// 设置是否允许玩家输入攻击信号 
        /// </summary>
        /// <param name="allow"></param>
        public void SetAllowAttackInput(bool allow) => allowAttackInput = allow;
        
        #endregion
    }
}

