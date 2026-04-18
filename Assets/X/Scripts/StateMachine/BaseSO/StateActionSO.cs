using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UGG.Move;
using UGG.Combat;
using UGG.Health;
using System.Security.Permissions;

public abstract class StateActionSO : ScriptableObject
{
    //����
    protected AICombatSystem _combatSystem;
    protected AIMovement _movement;
    protected AIHealthSystem _healthSystem;
    protected Animator _animator;
    protected Transform self;

    [SerializeField, Header("״̬���ȼ�")] protected int statePriority;//״̬���ȼ�


    //animationID
    protected int animationMoveID = Animator.StringToHash("AnimationMove");
    protected int movementID = Animator.StringToHash("Movement");
    protected int horizontalID = Animator.StringToHash("Horizontal");
    protected int verticalID = Animator.StringToHash("Vertical");
    protected int lAtkID = Animator.StringToHash("LAtk");
    protected int runID = Animator.StringToHash("Run");

    //�ƶ��ٶ�
    protected float walkSpeed = 1.5f;
    protected float runSpeed = 5f;
    [SerializeField] protected float currentMoveSpeed;

    public void InitState(StateMachineSystem stateMachineSystem)
    {
        _combatSystem = stateMachineSystem.GetComponentInChildren<AICombatSystem>();

        _movement = stateMachineSystem.GetComponent<AIMovement>();

        _healthSystem = stateMachineSystem.GetComponent<AIHealthSystem>();

        _animator = stateMachineSystem.GetComponentInChildren<Animator>();

        self = stateMachineSystem.transform;
    }

    protected void SetHorizontalAnimation(float value)
    {
        _animator.SetFloat(horizontalID, value);
        currentMoveSpeed = 0.85f;
    }

    protected void SetVerticalAnimation(float value)
    {
        _animator.SetFloat(verticalID, value);
        currentMoveSpeed = 1.5f;
    }


    protected void ResetAnimation()
    {
        currentMoveSpeed = 0f;
        _animator.SetFloat(verticalID, 0);
        _animator.SetFloat(horizontalID, 0);

    }

    public virtual void OnEnter() { }

    public abstract void OnUpdate();

    public virtual void OnExit() { }

    /// <summary>
    /// ��ȡ״̬���ȼ�
    /// </summary>
    /// <returns></returns>
    public int GetStatePriority() => statePriority;
}
