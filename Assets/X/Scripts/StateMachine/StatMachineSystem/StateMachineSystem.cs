using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StateMachineSystem : MonoBehaviour
{

    
    public NB_Transition transition;

    
    public StateActionSO currentState;


    private void Awake()
    {
        transition?.InitTransition(this);
        currentState?.OnEnter();
    }


    private void Update()
    {
        StateMachineTick();
    }

    private void StateMachineTick()
    {
        transition?.TryGetApplyCondition();//ÿһ֡��ȥ���Ƿ��г���������
        currentState?.OnUpdate();


    }
}
