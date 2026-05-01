using UnityEngine;
using UGG.Health;
using StarterAssets;
using UnityEngine.SceneManagement;
using System.Collections;
using UGG.Combat;

public class PauseMenuController : MonoBehaviour
{
    private const string TitleSceneName = "FirstScene";

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private PlayerHealthSystem playerHealth;

    private bool menuOpen;
    private bool deathMenuShown;
    private CanvasGroup menuCanvasGroup;
    private bool hideWithCanvasGroup;
    private TP_CameraController tpCameraController;
    private UnityTemplateProjects.SimpleCameraController simpleCameraController;
    private StarterAssetsInputs starterAssetsInputs;
    private PlayerCombatSystem playerCombatSystem;
    private CharacterInputSystem characterInputSystem;
    private bool starterCursorLockedBeforeMenu;
    private bool starterCursorInputForLookBeforeMenu;
    private bool tpCameraEnabledBeforeMenu;
    private bool simpleCameraEnabledBeforeMenu;
    private bool combatEnabledBeforeMenu;
    private bool inputEnabledBeforeMenu;
    private Coroutine restoreGameplayCoroutine;

    private void Awake()
    {
        menuOpen = false;
        deathMenuShown = false;
        tpCameraController = FindObjectOfType<TP_CameraController>(true);
        simpleCameraController = FindObjectOfType<UnityTemplateProjects.SimpleCameraController>(true);
        starterAssetsInputs = FindObjectOfType<StarterAssetsInputs>(true);
        playerCombatSystem = FindObjectOfType<PlayerCombatSystem>(true);
        characterInputSystem = FindObjectOfType<CharacterInputSystem>(true);

        if (menuPanel != null)
        {
            hideWithCanvasGroup = menuPanel == gameObject;

            if (hideWithCanvasGroup)
            {
                menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();

                if (menuCanvasGroup == null)
                {
                    menuCanvasGroup = menuPanel.AddComponent<CanvasGroup>();
                }
            }

            SetMenuVisible(false);
        }
    }

    private void Update()
    {
        if (!deathMenuShown && playerHealth != null && playerHealth.IsDead())
        {
            deathMenuShown = true;
            OpenMenu();
            return;
        }

        if (menuOpen)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        if (!deathMenuShown && Input.GetKeyDown(KeyCode.Tab))
        {
            if (menuOpen) CloseMenu();
            else OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (restoreGameplayCoroutine != null)
        {
            StopCoroutine(restoreGameplayCoroutine);
            restoreGameplayCoroutine = null;
        }

        menuOpen = true;
        SetMenuVisible(true);
        Time.timeScale = 0f;
        ReleaseCursorControl();
    }

    public void CloseMenu()
    {
        menuOpen = false;
        SetMenuVisible(false);
        Time.timeScale = 1f;

        if (restoreGameplayCoroutine != null)
        {
            StopCoroutine(restoreGameplayCoroutine);
        }

        restoreGameplayCoroutine = StartCoroutine(RestoreGameplayNextFrame());
    }

    public void ContinueGame()
    {
        CloseMenu();
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    private void ReleaseCursorControl()
    {
        if (playerCombatSystem != null)
        {
            combatEnabledBeforeMenu = playerCombatSystem.enabled;
            playerCombatSystem.enabled = false;
        }

        if (characterInputSystem != null)
        {
            inputEnabledBeforeMenu = characterInputSystem.enabled;
            characterInputSystem.enabled = false;
        }

        if (tpCameraController != null)
        {
            tpCameraEnabledBeforeMenu = tpCameraController.enabled;
            tpCameraController.enabled = false;
        }

        if (simpleCameraController != null)
        {
            simpleCameraEnabledBeforeMenu = simpleCameraController.enabled;
            simpleCameraController.enabled = false;
        }

        if (starterAssetsInputs != null)
        {
            starterCursorLockedBeforeMenu = starterAssetsInputs.cursorLocked;
            starterCursorInputForLookBeforeMenu = starterAssetsInputs.cursorInputForLook;
            starterAssetsInputs.cursorLocked = false;
            starterAssetsInputs.cursorInputForLook = false;
        }

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void RestoreCursorControl()
    {
        if (playerCombatSystem != null)
        {
            playerCombatSystem.enabled = combatEnabledBeforeMenu;
        }

        if (characterInputSystem != null)
        {
            characterInputSystem.enabled = inputEnabledBeforeMenu;
        }

        if (tpCameraController != null)
        {
            tpCameraController.enabled = tpCameraEnabledBeforeMenu;
        }

        if (simpleCameraController != null)
        {
            simpleCameraController.enabled = simpleCameraEnabledBeforeMenu;
        }

        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorLocked = starterCursorLockedBeforeMenu;
            starterAssetsInputs.cursorInputForLook = starterCursorInputForLookBeforeMenu;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator RestoreGameplayNextFrame()
    {
        yield return null;
        RestoreCursorControl();
        restoreGameplayCoroutine = null;
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuPanel == null)
        {
            return;
        }

        if (hideWithCanvasGroup && menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = visible ? 1f : 0f;
            menuCanvasGroup.interactable = visible;
            menuCanvasGroup.blocksRaycasts = visible;
            return;
        }

        menuPanel.SetActive(visible);
    }
}
