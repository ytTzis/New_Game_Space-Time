using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UGG.Health
{
    public class PlayerHealthSystem : CharacterHealthSystemBase
    {
        private const string DefaultDeathAnimation = "GhostSamurai_APose_Die01_Inplace";
        private const string DefaultDeathAnimationPath = "Assets/GameAssets/GreatSword_Animset/Animation/katana/APose/Die/Inplace/GhostSamurai_APose_Die01_Inplace.FBX";

        [Header("Player HP")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField, Header("受击锁定攻击者结束时间(0-1)")] [Range(0f, 1f)] private float hitLockReleaseNormalizedTime = 0.35f;

        private bool canExecute = false;
        private UGG.Move.PlayerMovementController playerMovementController;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

        protected override void Awake()
        {
            base.Awake();
            playerMovementController = GetComponent<UGG.Move.PlayerMovementController>();
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            if (string.IsNullOrEmpty(deathAnimationName))
            {
                deathAnimationName = DefaultDeathAnimation;
            }

            TryAssignDefaultDeathClip();
        }

        protected override void Update()
        {
            base.Update();

            OnHitLockTarget();
        }

        public override void TakeDamager(float damagar, string hitAnimationName, Transform attacker)
        {
            if (IsDead())
            {
                return;
            }

            if (playerMovementController != null && playerMovementController.IsDodgeInvulnerable())
            {
                return;
            }

            SetAttacker(attacker);

            if (CanParry())
            {
                Parry(hitAnimationName);
            }
            else
            {
                ApplyDamage(damagar);

                if (currentHealth <= 0f)
                {
                    Die();
                    return;
                }

                _animator.Play(hitAnimationName, 0, 0f);
                GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hit);
            }
        }

        public void RestoreFullHealth()
        {
            currentHealth = maxHealth;
        }

        #region Parry

        private bool CanParry()
        {
            if (_animator.CheckAnimationTag("Parry")) return true;
            if (_animator.CheckAnimationTag("ParryHit")) return true;

            return false;
        }

        private void Parry(string hitName)
        {
            if (!CanParry()) return;

            switch (hitName)
            {
                default:
                    _animator.Play(hitName, 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hit);
                    break;
                case "Hit_D_Up":
                    //_animator.Play("ParryF", 0, 0f);
                    //GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);

                    if(currentAttacker.TryGetComponent(out CharacterHealthSystemBase health))
                    {
                        health.FlickWeapon("Flick_0");
                        GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    }

                    canExecute = true;

                    //游戏时间缓慢 给玩家处决反应时间
                    Time.timeScale = 0.25f;
                    GameObjectPoolSystem.Instance.TakeGameObject("Timer").GetComponent<Timer>().CreateTime(0.25f, () =>
                    {
                        canExecute = false;

                        if (Time.timeScale < 1f)
                        {
                            Time.timeScale = 1f;
                        }
                    }, false);
                    break;
                case "Hit_H_Right":
                    _animator.Play("ParryL", 0, 0f);
                    GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.parry);
                    break;
            }
        }

        #endregion

        #region Hit

        private bool CanHitLockAttacker()
        {
            return true;
        }

        private void OnHitLockTarget()
        {
            //检测当前动画是否处于受伤状态
            bool isHitLocked = _animator.CheckAnimationTag("Hit") &&
                               !_animator.CheckCurrentTagAnimationTimeIsExceed("Hit", hitLockReleaseNormalizedTime);
            bool isParryHitLocked = _animator.CheckAnimationTag("ParryHit");

            if (isHitLocked || isParryHitLocked)
            {
                transform.rotation = transform.LockOnTarget(currentAttacker, transform, 50f);
            }
        }

        private void ApplyDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
            }
        }

        #endregion

        public bool GetCanExecute() => canExecute;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(deathAnimationName))
            {
                deathAnimationName = DefaultDeathAnimation;
            }

            TryAssignDefaultDeathClip();
        }
#endif

        private void TryAssignDefaultDeathClip()
        {
            if (deathAnimationClip != null)
            {
                return;
            }

#if UNITY_EDITOR
            deathAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DefaultDeathAnimationPath);
#endif
        }
    }
}

