using UnityEngine;
using UnityEngine.UI;
using MediProfen.Core;
using MediProfen.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MediProfen.UI
{
    public class MenuScenarioSelector : MonoBehaviour
    {
        [SerializeField] private ScenarioDatabase database;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        private int currentIndex;

        private void Start()
        {
            if (database == null && GameFlowManager.Instance != null)
            {
                database = GameFlowManager.Instance.ScenarioDatabase;
            }

            RefreshUI();
        }

        public void Next()
        {
            if (database == null || database.Scenarios.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex + 1) % database.Scenarios.Count;
            RefreshUI();
        }

        public void Previous()
        {
            if (database == null || database.Scenarios.Count == 0)
            {
                return;
            }

            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = database.Scenarios.Count - 1;
            }

            RefreshUI();
        }

        public void Play()
        {
            Debug.Log("[MenuScenarioSelector] Play clicked.");
            if (database == null || database.Scenarios.Count == 0)
            {
                Debug.LogWarning("[MenuScenarioSelector] No ScenarioDatabase assigned or list is empty.");
                return;
            }

            var scenario = database.Scenarios[currentIndex];
            if (scenario == null || GameFlowManager.Instance == null)
            {
                Debug.LogWarning("[MenuScenarioSelector] Scenario is null or GameFlowManager missing.");
                return;
            }

            Debug.Log($"[MenuScenarioSelector] Selected scenario: {scenario.DisplayName} | Scene: {scenario.SceneName}");

            GameFlowManager.Instance.SelectScenario(scenario);
            GameFlowManager.Instance.StartSelectedScenario();
        }

        public void Quit()
        {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }

        private void RefreshUI()
        {
            if (database == null || database.Scenarios.Count == 0)
            {
                if (titleText != null)
                {
                    titleText.text = "No Scenarios";
                }

                if (descriptionText != null)
                {
                    descriptionText.text = "";
                }

                return;
            }

            var scenario = database.Scenarios[currentIndex];
            if (titleText != null)
            {
                titleText.text = scenario != null ? scenario.DisplayName : "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = scenario != null ? scenario.Description : "";
            }
        }
    }
}
