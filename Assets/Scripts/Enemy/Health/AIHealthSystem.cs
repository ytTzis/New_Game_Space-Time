using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UGG.Health
{
    public class AIHealthSystem : CharacterHealthSystemBase
    {
        [SerializeField] private int maxParryCount;
        [SerializeField] private int counterattackParryCount;//当格挡次数大于设置的值 触发反击技能

        [SerializeField] private int maxHitCount;
        [SerializeField] private int hitCount;//如果受伤次数超过最大受伤次数 触发脱身技能

        private void Start()
        {
            hitCount = 0;
        }

        private void LateUpdate()
        {
            OnHitLockTarget();
        }

        public override void TakeDamager(float damagar, string hitAnimationName, Transform attacker)
        {
            SetAttacker(attacker);

            if (maxParryCount > 0 && !OnInvincibleState())
            {
                //如果反击格挡次数等于2
                if (counterattackParryCount == 2)
                {
                    //触发反击技能
                    _animator.Play("CounterAttack", 0, 0f);
                    counterattackParryCount = 0;
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                }
                else
                {
                    OnParry(hitAnimationName);
                }
                maxParryCount--;
            }
            else
            {
                if (hitCount == maxHitCount && !_animator.CheckAnimationTag("Flick_0"))
                {
                    //触发脱身技能
                    _animator.Play("Roll_B", 0, 0f);

                    hitCount = 0;
                    maxHitCount += Random.Range(1, 4);
                }
                else
                {
                    if (!OnInvincibleState())
                    {
                        _animator.Play(hitAnimationName, 0, 0f);
                        GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hit);
                        hitCount++;
                    }
                } 
            }
        }

        /// <summary>
        /// 处于处决状态无敌不受到伤害
        /// </summary>
        private bool OnInvincibleState()
        {
            if (_animator.CheckAnimationTag("CounterAttack")) return true;

            return false;
        }

        private void OnHitLockTarget()
        {
            //检测当前动画是否处于受伤状态
            if (_animator.CheckAnimationTag("Hit"))
            {
                transform.rotation = transform.LockOnTarget(currentAttacker, transform, 50f);
            }
        }

        private void OnParry(string hitName)
        {
            switch (hitName)
            {
                default:
                    _animator.Play(hitName, 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hit);
                    break;
                case "Hit_D_Up":
                    _animator.Play("ParryF", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    counterattackParryCount++;
                    break;
                case "Hit_H_Left":
                    _animator.Play("ParryR", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    counterattackParryCount++;
                    break;
                case "Hit_H_Right":
                    _animator.Play("ParryL", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    counterattackParryCount++;
                    break;
                case "Hit_Up_Left":
                    _animator.Play("ParryR", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    counterattackParryCount++;
                    break;
                case "Hit_Up_Right":
                    _animator.Play("ParryL", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    counterattackParryCount++;
                    break;
            }
        }
    }

}