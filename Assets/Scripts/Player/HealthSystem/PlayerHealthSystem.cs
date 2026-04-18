using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UGG.Health
{
    public class PlayerHealthSystem : CharacterHealthSystemBase
    {
        private bool canExecute = false;

        protected override void Update()
        {
            base.Update();

            OnHitLockTarget();
        }

        public override void TakeDamager(float damagar, string hitAnimationName, Transform attacker)
        {
            SetAttacker(attacker);

            if (CanParry())
            {
                Parry(hitAnimationName);
            }
            else
            {
                _animator.Play(hitAnimationName, 0, 0f);
                GameAssets.Instance.PlaySoundEffect(_audioSource, SoundAssetsType.hit);
            }
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
            if (_animator.CheckAnimationTag("Hit") || _animator.CheckAnimationTag("ParryHit"))
            {
                transform.rotation = transform.LockOnTarget(currentAttacker, transform, 50f);
            }
        }

        #endregion

        public bool GetCanExecute() => canExecute;
    }
}

