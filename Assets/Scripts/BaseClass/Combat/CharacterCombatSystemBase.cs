using System;
using System.Collections;
using System.Collections.Generic;
using UGG.Move;
using UnityEngine;

namespace UGG.Combat
{
    public abstract class CharacterCombatSystemBase : MonoBehaviour
    {
        protected Animator _animator;
        protected CharacterInputSystem _characterInputSystem;
        protected CharacterMovementBase _characterMovementBase;
        protected AudioSource _audioSource;

        //aniamtionID
        protected int lAtkID = Animator.StringToHash("LAtk");
        protected int rAtkID = Animator.StringToHash("RAtk");
        protected int defenID = Animator.StringToHash("Parry");
        protected int animationMoveID = Animator.StringToHash("AnimationMove");
        protected int secondaryWeaponID = Animator.StringToHash("SecondaryWeapon");

        //攻击检测
        [SerializeField, Header("攻击检测")] protected Transform attackDetectionCenter;
        [SerializeField] protected float attackDetectionRang;
        [SerializeField] protected LayerMask enemyLayer;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _characterInputSystem = GetComponentInParent<CharacterInputSystem>();
            _characterMovementBase = GetComponentInParent<CharacterMovementBase>();
            _audioSource = _characterMovementBase.GetComponentInChildren<AudioSource>();
        }

        /// <summary>
        /// 攻击动画攻击检测事件
        /// </summary>
        /// <param name="hitName">传递受伤动画名</param>
        protected virtual void OnAnimationAttackEvent(string hitName)
        {
            Collider[] attackDetectionTargets = new Collider[4];

            int counts = Physics.OverlapSphereNonAlloc(attackDetectionCenter.position, attackDetectionRang, attackDetectionTargets, enemyLayer);

            if (counts > 0)
            {
                for (int i = 0; i < counts; i++)
                {
                    if (attackDetectionTargets[i].TryGetComponent(out IDamagar damagar))
                    {
                        damagar.TakeDamager(0, hitName, transform.root.transform);
                    }
                }
            }
            PlayerWeaponEffect();
        }

        private void PlayerWeaponEffect()
        {
            if (_animator.CheckAnimationTag("Attack"))
            {
                GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.swordWave);
            }

            if (_animator.CheckAnimationTag("GSAttack"))
            {
                GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hSwordWave);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(attackDetectionCenter.position, attackDetectionRang);
        }
    }
}
