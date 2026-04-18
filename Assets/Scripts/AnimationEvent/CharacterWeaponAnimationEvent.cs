using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterWeaponAnimationEvent : MonoBehaviour
{
    [SerializeField] private Transform hipGS;
    [SerializeField] private Transform handGS;
    [SerializeField] private Transform handKatana;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HitHideGS();
    }

    /// <summary>
    /// 显示大剑
    /// </summary>
    public void ShowGS()
    {
        //如果手部大剑是隐藏状态
        if (!handGS.gameObject.activeSelf)
        {
            //显示手部大剑
            handGS.gameObject.SetActive(true);

            //隐藏背部大剑
            hipGS.gameObject.SetActive(false);

            //隐藏手部默认刀
            handKatana.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏大剑
    /// </summary>
    public void HideGS()
    {
        //如果手部大剑是显示状态
        if (handGS.gameObject.activeSelf)
        {
            //隐藏手部大剑
            handGS.gameObject.SetActive(false);

            //显示背部大剑
            hipGS.gameObject.SetActive(true);

            //显示手部默认刀
            handKatana.gameObject.SetActive(true);
        }
    }

    private void HitHideGS()
    {
        if (animator.CheckAnimationTag("Hit") || animator.CheckAnimationTag("ParryHit"))
        {
            handKatana.gameObject.SetActive(true);

            hipGS.gameObject.SetActive(true);
            handGS.gameObject.SetActive(false);
        }
    }
}
