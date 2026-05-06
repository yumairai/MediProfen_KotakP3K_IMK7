using UnityEngine;
using UnityEngine.SceneManagement;
using MediProfen.Data;
using MediProfen.Objectives;

namespace MediProfen.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private ScenarioDatabase scenarioDatabase;

        [Header("Settings")]
        [SerializeField] private string menuSceneName = "Main VR Scene";

        public GameState State { get; private set; } = GameState.Menu;
        public ScenarioData SelectedScenario { get; private set; }

        public ScenarioDatabase ScenarioDatabase => scenarioDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void SelectScenario(ScenarioData scenario)
        {
            SelectedScenario = scenario;
        }

        public void StartSelectedScenario()
        {
            if (SelectedScenario == null)
            {
                Debug.LogWarning("No scenario selected.");
                return;
            }

            State = GameState.LoadingScenario;
            SceneManager.LoadSceneAsync(SelectedScenario.SceneName);
        }

        public void ReturnToMenu()
        {
            SelectedScenario = null;
            State = GameState.LoadingScenario;
            SceneManager.LoadSceneAsync(menuSceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SelectedScenario != null && scene.name == SelectedScenario.SceneName)
            {
                var runner = FindAnyObjectByType<ObjectiveRunner>();
                if (runner != null)
                {
                    runner.BeginScenario(SelectedScenario);
                    State = GameState.InScenario;
                }
                else
                {
                    Debug.LogWarning("ObjectiveRunner not found in scenario scene.");
                }

                return;
            }

            if (scene.name == menuSceneName)
            {
                State = GameState.Menu;
            }
        }
    }
}
