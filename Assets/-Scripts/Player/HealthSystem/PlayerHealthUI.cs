using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGG.Health
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealthSystem playerHealthSystem;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Display")]
        [SerializeField] private bool updateSlider = true;
        [SerializeField] private bool updateFillImage = true;
        [SerializeField] private bool updateHealthText = true;
        [SerializeField] private bool useNormalizedValue = true;

        private void Awake()
        {
            TryAutoBindPlayerHealth();
            RefreshUI();
        }

        private void Update()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (playerHealthSystem == null)
            {
                return;
            }

            float normalizedHealth = playerHealthSystem.HealthNormalized;
            float currentHealth = playerHealthSystem.CurrentHealth;
            float maxHealth = playerHealthSystem.MaxHealth;

            if (updateSlider && healthSlider != null)
            {
                if (useNormalizedValue)
                {
                    healthSlider.minValue = 0f;
                    healthSlider.maxValue = 1f;
                    healthSlider.value = normalizedHealth;
                }
                else
                {
                    healthSlider.minValue = 0f;
                    healthSlider.maxValue = maxHealth;
                    healthSlider.value = currentHealth;
                }
            }

            if (updateFillImage && healthFillImage != null)
            {
                healthFillImage.fillAmount = normalizedHealth;
            }

            if (updateHealthText && healthText != null)
            {
                healthText.text = $"{currentHealth:0} / {maxHealth:0}";
            }
        }

        private void TryAutoBindPlayerHealth()
        {
            if (playerHealthSystem != null)
            {
                return;
            }

            playerHealthSystem = FindFirstObjectByType<PlayerHealthSystem>();
        }
    }
}
