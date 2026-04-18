using System;
using System.Collections.Generic;
using UnityEngine;

//核心脚本
//实现功能如下
//游戏开始前先测量基线心率HR_case
//计算最近5秒平均值
//计算上一个5秒平均值
//判断是否为突发紧张
//判断是否为持续紧张
//判断是否恢复冷静
//状态切换：Calm/Alert/Tense/Panic 

public class HeartRateArousalSystem : MonoBehaviour
{
    public enum ArousalState
    {
        Calm,
        Alert,
        Tense,
        Panic
    }

    [Header("Calibration")]
    public float calibrationDuration = 20f;
    public bool autoStartCalibration = true;

    [Header("Window Settings")]
    public float shortWindow = 5f;
    public float longWindow = 15f;
    public float sampleInterval = 0.2f;

    [Header("Dynamic Thresholds")]
    [Tooltip("最近5秒相比前一个5秒增长超过15%，算突发紧张")]
    public float suddenIncreaseThreshold = 0.15f;

    [Tooltip("当前均值超过基线12%，进入持续紧张判定")]
    public float sustainedAboveBaselineThreshold = 0.12f;

    [Tooltip("当前均值低于基线5%，视为恢复冷静参考")]
    public float recoverThreshold = 0.05f;

    [Header("Timers")]
    public float sustainedTenseTimeNeeded = 8f;
    public float recoverTimeNeeded = 10f;
    public float panicHoldTime = 1.5f;

    [Header("Runtime Readonly")]
    public float currentHeartRate;
    public float baselineHeartRate;
    public float shortAverage;
    public float previousShortAverage;
    public float longAverage;
    public bool isCalibrating = false;
    public bool suddenTensionTriggered = false;
    public ArousalState currentState = ArousalState.Alert;

    public Action<ArousalState> OnStateChanged;
    public Action OnSuddenTension;

    private float sampleTimer = 0f;
    private float calibrationTimer = 0f;
    private float sustainedHighTimer = 0f;
    private float recoverTimer = 0f;
    private float panicTimer = 0f;

    private float shortWindowTimer = 0f;
    private float previousShortWindowTimer = 0f;

    private Queue<float> shortSamples = new Queue<float>();
    private Queue<float> longSamples = new Queue<float>();
    private Queue<float> previousShortSamples = new Queue<float>();

    private ArousalState lastState;

    void Start()
    {
        if (autoStartCalibration)
        {
            StartCalibration();
        }
        else
        {
            baselineHeartRate = 75f;
            currentState = ArousalState.Alert;
            lastState = currentState;
        }
    }

    public void StartCalibration()
    {
        isCalibrating = true;
        calibrationTimer = 0f;
        baselineHeartRate = 0f;
        sustainedHighTimer = 0f;
        recoverTimer = 0f;
        panicTimer = 0f;

        shortSamples.Clear();
        longSamples.Clear();
        previousShortSamples.Clear();

        shortWindowTimer = 0f;
        previousShortWindowTimer = 0f;
    }

    void Update()
    {
        //直接读取hyperateSocket 脚本里的实时心率
        currentHeartRate = hyperateSocket.CurrentHeartRate;

        //防止刚连接时心率还是 0，导致平均值和基线异常
        if (currentHeartRate <= 0f)
            return;

        sampleTimer += Time.deltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            AddSample(currentHeartRate);
        }

        if (isCalibrating)
        {
            UpdateCalibration();
            return;
        }

        UpdateArousalLogic();
        UpdateState();
    }

    void UpdateCalibration()
    {
        calibrationTimer += Time.deltaTime;

        if (longSamples.Count > 0)
        {
            baselineHeartRate = AverageQueue(longSamples);
        }

        if (calibrationTimer >= calibrationDuration)
        {
            isCalibrating = false;
            baselineHeartRate = Mathf.Max(1f, AverageQueue(longSamples));
            currentState = ArousalState.Alert;
            lastState = currentState;

            Debug.Log("Calibration Finished. Baseline HR = " + baselineHeartRate);
        }
    }

    void AddSample(float hr)
    {
        shortSamples.Enqueue(hr);
        longSamples.Enqueue(hr);

        shortWindowTimer += sampleInterval;
        previousShortWindowTimer += sampleInterval;

        while (shortWindowTimer > shortWindow && shortSamples.Count > 0)
        {
            float moved = shortSamples.Dequeue();
            previousShortSamples.Enqueue(moved);
            shortWindowTimer -= sampleInterval;
        }

        while (previousShortWindowTimer > shortWindow && previousShortSamples.Count > 0)
        {
            previousShortSamples.Dequeue();
            previousShortWindowTimer -= sampleInterval;
        }

        int maxLongCount = Mathf.CeilToInt(longWindow / sampleInterval);
        while (longSamples.Count > maxLongCount)
        {
            longSamples.Dequeue();
        }

        shortAverage = AverageQueue(shortSamples);
        previousShortAverage = AverageQueue(previousShortSamples);
        longAverage = AverageQueue(longSamples);
    }

    void UpdateArousalLogic()
    {
        suddenTensionTriggered = false;

        float baselineDelta = 0f;
        if (baselineHeartRate > 0.01f)
            baselineDelta = (shortAverage - baselineHeartRate) / baselineHeartRate;

        float shortIncreaseRatio = 0f;
        if (previousShortAverage > 0.01f)
            shortIncreaseRatio = (shortAverage - previousShortAverage) / previousShortAverage;

        // 突发紧张判定
        if (shortIncreaseRatio >= suddenIncreaseThreshold)
        {
            suddenTensionTriggered = true;
            panicTimer = panicHoldTime;
            OnSuddenTension?.Invoke();

            // ===== 这里填写“突发紧张”时要执行的功能 =====
            //
            //
            //
            //
            //
            // ===========================================
        }

        // 持续紧张判定
        if (baselineDelta >= sustainedAboveBaselineThreshold)
        {
            sustainedHighTimer += Time.deltaTime;
            recoverTimer = 0f;

            // =====这里填写“持续紧张期间”要执行的功能 =====
            //
            //
            //
            //
            // =============================================
        }
        else if (baselineDelta <= recoverThreshold)
        {
            recoverTimer += Time.deltaTime;
            sustainedHighTimer = 0f;
        }
        else
        {
            sustainedHighTimer = Mathf.Max(0f, sustainedHighTimer - Time.deltaTime * 0.5f);
            recoverTimer = 0f;
        }

        if (panicTimer > 0f)
        {
            panicTimer -= Time.deltaTime;
        }
    }

    void UpdateState()
    {
        ArousalState newState = currentState;

        if (panicTimer > 0f)
        {
            newState = ArousalState.Panic;
        }
        else if (shortAverage < baselineHeartRate * 0.95f && recoverTimer >= recoverTimeNeeded)
        {
            newState = ArousalState.Calm;
        }
        else if (sustainedHighTimer >= sustainedTenseTimeNeeded)
        {
            newState = ArousalState.Tense;
        }
        else
        {
            newState = ArousalState.Alert;
        }

        currentState = newState;

        if (currentState != lastState)
        {
            lastState = currentState;
            OnStateChanged?.Invoke(currentState);
            Debug.Log("Arousal State Changed => " + currentState);
        }
    }

    float AverageQueue(Queue<float> queue)
    {
        if (queue.Count == 0) return 0f;

        float sum = 0f;
        foreach (var v in queue)
            sum += v;

        return sum / queue.Count;
    }
}