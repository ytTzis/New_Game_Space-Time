using System;
using UnityEngine;

namespace UGG.Move
{
    public class PlayerMovementController : CharacterMovementBase
    {
        //引用
        private Transform characterCamera;
        private TP_CameraController _tpCameraController;

        [SerializeField, Header("相机站立锁定点")] private Transform standCameraLook;
        [SerializeField, Header("相机下蹲锁定点")] private Transform crouchCameraLook;

        //Ref Value
        private float targetRotation;
        private float rotationVelocity;

        //LerpTime
        [SerializeField, Header("旋转速度")] private float rotationLerpTime;
        [SerializeField, Header("旋转方向的平滑时间")] private float moveDirctionSlerpTime;


        //Move Speed
        [SerializeField, Header("行走速度")] private float walkSpeed;
        [SerializeField, Header("奔跑速度")] private float runSpeed;
        [SerializeField, Header("下蹲速度")] private float crouchMoveSpeed;

        [SerializeField, Header("动画移动速度倍率")] private float animationMoveSpeedMult;


        [SerializeField, Header("角色胶囊控制(下蹲)")] private Vector3 crouchCenter;
        [SerializeField] private Vector3 originCenter;
        [SerializeField] private Vector3 cameraLookPositionOnCrouch;
        [SerializeField] private Vector3 cameraLookPositionOrigin;
        [SerializeField] private float crouchHeight;
        [SerializeField] private float originHeight;
        [SerializeField] private bool isOnCrouch;
        [SerializeField] private Transform crouchDetectionPosition;
        [SerializeField] private Transform CameraLook;
        [SerializeField] private LayerMask crouchDetectionLayer;

        //AnimationID
        private int crouchID = Animator.StringToHash("Crouch");

        #region 内部函数

        protected override void Awake()
        {
            base.Awake();

            characterCamera = Camera.main.transform.root.transform;
            _tpCameraController = characterCamera.GetComponent<TP_CameraController>();
        }

        protected override void Start()
        {
            base.Start();

            cameraLookPositionOrigin = CameraLook.position;
        }

        protected override void Update()
        {
            base.Update();
            
            PlayerMoveDirection();
            UpdateRollAnimation();
        }

        private void LateUpdate()
        {
            CharacterCrouchControl();
            UpdateMotionAnimation();
            UpdateCrouchAnimation();
            UpdateRollAnimation();
        }

        #endregion

        #region 条件

        private bool CanMoveContro()
        {
            return isOnGround && characterAnimator.CheckAnimationTag("Motion") || characterAnimator.CheckAnimationTag("CrouchMotion");
        }

        private bool CanCrouch()
        {
            if (characterAnimator.CheckAnimationTag("Crouch")) return false;
            if (characterAnimator.GetFloat(runID)>.9f) return false;
            
            return true;
        }
        
        private bool CanRunControl()
        {
            if (Vector3.Dot(movementDirection.normalized, transform.forward) < 0.75f) return false;
            if (!CanMoveContro()) return false;
          
            return true;
        }

        #endregion
        
        private void PlayerMoveDirection()
        {
            //如果在地面 并且按键没有输入 就把移动方向设置为0
            if (isOnGround && _inputSystem.playerMovement == Vector2.zero)
                movementDirection = Vector3.zero;
            
            //如果可以移动
            if(CanMoveContro()) 
            {
                //如果玩家输入不为0
                if (_inputSystem.playerMovement != Vector2.zero)
                {
                    //将玩家X输入轴和Y输入轴的弧度转化为角度并加上摄像机自身Y轴的旋转 使摄像机跟着角色旋转
                    targetRotation = Mathf.Atan2(_inputSystem.playerMovement.x, _inputSystem.playerMovement.y) * Mathf.Rad2Deg + characterCamera.localEulerAngles.y;

                    //玩家旋转平滑
                    transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationLerpTime);

                    //旋转并始终向Z轴移动的向量
                    var direction = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;

                    //将移动方向标准化
                    direction = direction.normalized;

                    //优化：让角色突然换方向更流畅
                    movementDirection = Vector3.Slerp(movementDirection, ResetMoveDirectionOnSlop(direction), moveDirctionSlerpTime * Time.deltaTime);
                }
            }
            else 
            {
                //如果不能移动 则把移动向量设置为0
                movementDirection = Vector3.zero;
            }

            //玩家移动
            //优化：先乘浮点再对向量进行运算可以增加运算速度
            control.Move((characterCurrentMoveSpeed * Time.deltaTime) * movementDirection.normalized + Time.deltaTime * new Vector3(0.0f, verticalSpeed, 0.0f));
            //control.Move(movementDirection.normalized * (characterCurrentMoveSpeed * Time.deltaTime) + new Vector3(0.0f, verticalSpeed, 0.0f) * Time.deltaTime);
        }

        private void UpdateMotionAnimation()
        {
            //如果可以奔跑
            if (CanRunControl())
            {
                characterAnimator.SetFloat(movementID, _inputSystem.playerMovement.sqrMagnitude * ((_inputSystem.playerRun && !isOnCrouch) ? 2f : 1f), 0.1f, Time.deltaTime);

                //如果玩家按下奔跑键并且不是下蹲状态就把角色当前移动速度设置为奔跑速度 如果没有按下奔跑键并且不是下蹲状态则设置为行走速度
                characterCurrentMoveSpeed = (_inputSystem.playerRun && !isOnCrouch) ? runSpeed : walkSpeed;
            }
            else
            {
                characterAnimator.SetFloat(movementID, 0f, 0.05f, Time.deltaTime);
                characterCurrentMoveSpeed = 0f;
            }

            characterAnimator.SetFloat(runID, (_inputSystem.playerRun && !isOnCrouch) ? 1f : 0f);
        }

        private void UpdateCrouchAnimation()
        {
            //如果角色处于下蹲状态
            if (isOnCrouch)
            {
                //将角色当前移动速度设置为下蹲时的移动速度
                characterCurrentMoveSpeed = crouchMoveSpeed;
            }
        }

        private void UpdateRollAnimation()
        {
            //如果玩家按下翻滚键
            if (_inputSystem.playerRoll)
            {
                //设置翻滚动画
                characterAnimator.SetTrigger(rollID);
            }

            //检测是否在翻滚状态
            if (characterAnimator.CheckAnimationTag("Roll"))
            {
                CharacterMoveInterface(transform.forward, characterAnimator.GetFloat(animationMoveID) * animationMoveSpeedMult, true);
            }
        }
        
        private void CharacterCrouchControl()
        {
            //如果角色不能下蹲 则返回
            if (!CanCrouch()) return;

            //如果角色按下蹲键
            if (_inputSystem.playerCrouch)
            {
                //判断现在是否处于下蹲状态
                if (isOnCrouch)
                {
                    //检测头顶是否有障碍物 取反
                    if (!DetectionHeadHasObject())
                    {
                        isOnCrouch = false;
                        characterAnimator.SetFloat(crouchID, 0f);
                        SetCrouchColliderHeight(originHeight, originCenter);
                        _tpCameraController.SetLookPlayerTarget(standCameraLook);
                    }
                }
                else
                {
                    isOnCrouch = true;
                    characterAnimator.SetFloat(crouchID, 1f);
                    SetCrouchColliderHeight(crouchHeight, crouchCenter);
                    _tpCameraController.SetLookPlayerTarget(crouchCameraLook);
                }
            }
        }
        
        private void SetCrouchColliderHeight(float height,Vector3 center)
        {
            control.center = center;
            control.height = height;
        }
        
        /// <summary>
        /// 检测头顶是否有障碍物
        /// </summary>
        /// <returns></returns>
        private bool DetectionHeadHasObject()
        {
            Collider[] hasObjects = new Collider[1];

            int objectCount = Physics.OverlapSphereNonAlloc(crouchDetectionPosition.position, 0.5f, hasObjects, crouchDetectionLayer);
            
            if (objectCount > 0)
            {
                return true;
            }

            return false;
        }
    }
}
