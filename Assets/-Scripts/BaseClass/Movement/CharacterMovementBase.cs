using Unity.Collections;
using UnityEngine;


namespace UGG.Move
{
    //Base class for all roles 角色基类(所有角色，玩家 . 敌人)
    public abstract class CharacterMovementBase : MonoBehaviour
    {
        //引用
        protected Animator characterAnimator;
        protected CharacterController control;
        protected CharacterInputSystem _inputSystem;

        //MoveDirection(移动向量)
        protected Vector3 movementDirection;
        protected Vector3 verticalDirection;


        [SerializeField, Header("角色重力")] protected float characterGravity;
        [SerializeField, Header("角色当前移动速度")] protected float characterCurrentMoveSpeed;
        protected float characterFallTime = 0.15f;//角色下落时间
        protected float characterFallOutDeltaTime;
        protected float verticalSpeed;//当前角色Y轴速度
        protected float maxVerticalSpeed = 53f;//角色Y轴最大速度


        [SerializeField, Header("地面检测")] protected float groundDetectionRang;
        [SerializeField] protected float groundDetectionOffset;
        [SerializeField] protected float slopRayExtent;
        [SerializeField] protected LayerMask whatIsGround;
        [SerializeField, Tooltip("角色动画移动时检测障碍物的层级")] protected LayerMask whatIsObs;
        [SerializeField] protected bool isOnGround;


        //AnimationID
        protected int animationMoveID = Animator.StringToHash("AnimationMove");
        protected int movementID = Animator.StringToHash("Movement");
        protected int horizontalID = Animator.StringToHash("Horizontal");
        protected int verticalID = Animator.StringToHash("Vertical");
        protected int runID = Animator.StringToHash("Run");
        protected int rollID = Animator.StringToHash("Roll");

        protected virtual void Awake()
        {
            characterAnimator = GetComponentInChildren<Animator>();
            control = GetComponent<CharacterController>();
            _inputSystem = GetComponent<CharacterInputSystem>();
            

        }
        
        protected virtual void Start()
        {
            characterFallOutDeltaTime = characterFallTime;
        }

        protected virtual void Update()
        {
            CheckOnGround();
            CharacterGravity();
        }

        #region 内部函数
        
        /// <summary>
        /// 角色重力
        /// </summary>
        private void CharacterGravity()
        {
            //如果角色处于地面
            if (isOnGround)
            {
                //重置下落时间
                characterFallOutDeltaTime = characterFallTime;

                //在地面时阻止速度无限下降
                if (verticalSpeed < 0.0f)
                {
                    verticalSpeed = -2f;
                }
            }
            else
            {
                //如果角色下落时间大于0 就证明处于下落状态
                if (characterFallOutDeltaTime >= 0.0f)
                {
                    //限制下落时间
                    characterFallOutDeltaTime = Mathf.Clamp(characterFallOutDeltaTime, 0f, characterFallTime);
                    //随着时间的推移减少
                    characterFallOutDeltaTime -= Time.deltaTime;
                }
            }

            //如果当前角色Y轴速度小于最大Y轴速度
            if (verticalSpeed < maxVerticalSpeed)
            {
                //重力加速度
                verticalSpeed += characterGravity * Time.deltaTime;
            }
        }

        /// <summary>
        /// 地面检测
        /// </summary>
        private void CheckOnGround()
        {
            //设置球体检测位置 在玩家当前的Y轴方向稍微向下偏移
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundDetectionOffset, transform.position.z);
            //开始检测
            isOnGround = Physics.CheckSphere(spherePosition, groundDetectionRang, whatIsGround, QueryTriggerInteraction.Ignore);
            
        }

        private void OnDrawGizmosSelected()
        {
            
            if (isOnGround) 
                Gizmos.color = Color.green;
            else 
                Gizmos.color = Color.red;

            Vector3 position = Vector3.zero;
            
            position.Set(transform.position.x, transform.position.y - groundDetectionOffset,
                transform.position.z);

            Gizmos.DrawWireSphere(position, groundDetectionRang);
            
        }

        /// <summary>
        /// 坡度检测
        /// </summary>
        /// <param name="dir">当前移动方向</param>
        /// <returns></returns>
        protected Vector3 ResetMoveDirectionOnSlop(Vector3 dir)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out var hit, slopRayExtent))
            {
                //计算角色上方与射线碰撞到的法向量点积
                float newAnle = Vector3.Dot(Vector3.up, hit.normal);

                //如果不等于0 并且角色当前Y轴速度小于等于0
                if (newAnle != 0 && verticalSpeed <= 0)
                {
                    //返回一个平面投影向量
                    return Vector3.ProjectOnPlane(dir, hit.normal);
                }
            }
            return dir;
        }

        protected bool CanAnimationMotion(Vector3 dir)
        {
            //检测前方是否有障碍物(characterAnimator.GetFloat(animationMoveID)速度防止在攻击前进时与敌人重叠)
            return Physics.Raycast(transform.position + transform.up * 0.5f, dir.normalized * characterAnimator.GetFloat(animationMoveID), out var hit, 1f, whatIsObs);
        }

        #endregion

        #region 公共函数

        /// <summary>
        /// 移动接口
        /// </summary>
        /// <param name="moveDirection">移动方向</param>
        /// <param name="moveSpeed">移动速度</param>
        public virtual void CharacterMoveInterface(Vector3 moveDirection, float moveSpeed, bool useGravity)
        {
            //如果移动方向的前方没有障碍物
            if (!CanAnimationMotion(moveDirection))
            {
                //移动方向标准化
                movementDirection = moveDirection.normalized;
        
                //对当前移动方向进行坡度检测
                movementDirection = ResetMoveDirectionOnSlop(movementDirection);

                //如果使用重力
                if (useGravity)
                {
                    //给垂直向量Y轴赋值
                    verticalDirection.Set(0.0f, verticalSpeed, 0.0f);
                }
                else
                {
                    //归零
                    verticalDirection = Vector3.zero;
                }

                //移动
                control.Move((moveSpeed * Time.deltaTime) * movementDirection.normalized + Time.deltaTime * verticalDirection);
            }
        }

        #endregion
        
        
    }
}
