using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NB_Transition", menuName = "StateMachine/Transition/New NB_Transition")]
public class NB_Transition : ScriptableObject
{
    [Serializable]
    private class StateAcitionConfig
    {
        public StateActionSO fromState;
        public StateActionSO toState;
        public List<ConditionSO> conditions;

        public void Init(StateMachineSystem stateMachineSystem)
        {
            fromState.InitState(stateMachineSystem);
            toState.InitState(stateMachineSystem);

            foreach (var item in conditions)
            {
                item.InitCondition(stateMachineSystem);
            }
        }
    }


    //存储所有状态转换信息和条件

    private Dictionary<StateActionSO, List<StateAcitionConfig>> states = new Dictionary<StateActionSO, List<StateAcitionConfig>>();

    //获取状态配置，即外部面板的手动配置信息
    [SerializeField] private List<StateAcitionConfig> configStateData = new List<StateAcitionConfig>();

    private StateMachineSystem stateMachineSystem;


    public void InitTransition(StateMachineSystem stateMachineSystem)
    {
        this.stateMachineSystem = stateMachineSystem;
        SaveAllStateTransitionInfo();
    }


    /// <summary>
    /// 保存所有状态配置信息
    /// </summary>
    private void SaveAllStateTransitionInfo()
    {
        foreach (var item in configStateData)
        {
            item.Init(stateMachineSystem);

            //这个时候外面面板已经配置好信息了。我们需要将它们的转换关系保存起来
            if (!states.ContainsKey(item.fromState))
            {
                //检测现在存储字典是否有存在的Key,如果没有我们需要创建一个，并且初始化它的条件存储容器
                states.Add(item.fromState, new List<StateAcitionConfig>());
                states[item.fromState].Add(item);
            }
            else
            {
                states[item.fromState].Add(item);
            }
        }
    }


    /// <summary>
    /// 尝试去获取条件成立的新状态
    /// </summary>
    public void TryGetApplyCondition()
    {
        int conditionPriority = 0;
        int statePriority = 0;
        List<StateActionSO> toStates = new List<StateActionSO>();
        StateActionSO toState = null;

        //遍历当前状态能转的状态是否有条件成立
        if (states.ContainsKey(stateMachineSystem.currentState))
        {
            foreach (var stateItem in states[stateMachineSystem.currentState])
            {
                foreach (var conditionItem in stateItem.conditions)
                {
                    if (conditionItem.ConditionSetUp())
                    {
                        if (conditionItem.GetConditionPriority() >= conditionPriority)
                        {
                            //那么就将转换关系中下一个状态保存起来。当所有都遍历完了，会存在一个唯一合适的状态
                            conditionPriority = conditionItem.GetConditionPriority();
                            toStates.Add(stateItem.toState);
                        }
                    }
                }
            }
        }
        else
        {
            return;
        }

        if (toStates.Count != 0 || toStates != null)
        {
            //遍历成立条件的优先级，返回优先级最高的哪那一个条件
            foreach (var item in toStates)
            {
                if (item.GetStatePriority() >= statePriority)
                {
                    //将下一个状态设置为优先级最高的那一个
                    statePriority = item.GetStatePriority();
                    toState = item;
                }
            }
        }

        if (toState != null)
        {
            stateMachineSystem.currentState.OnExit();
            stateMachineSystem.currentState = toState;
            stateMachineSystem.currentState.OnEnter();
            toStates.Clear();
            conditionPriority = 0;
            statePriority = 0;
            toState = null;
        }
    }



}
