using UnityEngine;

public class HeartRateEffectDemo : MonoBehaviour
{
    public HeartRateStateController stateController;

    private void Start()
    {
        if (stateController == null) return;

        stateController.OnRisingStressEnter += HandleRisingStress;
        stateController.OnHighStressEnter += HandleHighStress;
        stateController.OnRecoveringEnter += HandleRecovering;
        stateController.OnReturnToNormal += HandleReturnToNormal;
    }

    private void HandleRisingStress()
    {
        Debug.Log("触发：持续紧张 / 心率正在上升 / 轻度压迫");
    }

    private void HandleHighStress()
    {
        Debug.Log("触发：高度紧张 / 高压状态 / 体力消耗增加 / 仇恨范围增大");
    }

    private void HandleRecovering()
    {
        Debug.Log("触发：恢复冷静 / 心率正在回落");
    }

    private void HandleReturnToNormal()
    {
        Debug.Log("触发：恢复正常");
    }
}
