using System.Collections;
using MediProfen.Objectives;
using UnityEngine;

namespace MediProfen.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class BGMManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ObjectiveRunner pada scene. Jika kosong, akan dicari otomatis.")]
        public ObjectiveRunner objectiveRunner;
        [Tooltip("AudioSource untuk BGM. Jika kosong, akan memakai AudioSource di object ini.")]
        public AudioSource bgmSource;

        [Header("Clips")]
        [Tooltip("BGM yang diputar selama scenario berjalan.")]
        public AudioClip normalBgm;
        [Tooltip("BGM yang diputar setelah semua objective selesai.")]
        public AudioClip completeBgm;

        [Header("Settings")]
        public bool playNormalBgmOnStart = true;
        public float fadeDuration = 1.0f;
        public float normalVolume = 1.0f;
        public float completeVolume = 1.0f;

        private Coroutine transitionRoutine;
        private bool isCompleteBgmPlaying = false;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (playNormalBgmOnStart && normalBgm != null)
            {
                PlayClip(normalBgm, normalVolume, true);
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (objectiveRunner != null)
            {
                objectiveRunner.ScenarioCompleted += HandleScenarioCompleted;
            }
        }

        private void OnDisable()
        {
            if (objectiveRunner != null)
            {
                objectiveRunner.ScenarioCompleted -= HandleScenarioCompleted;
            }
        }

        private void ResolveReferences()
        {
            if (bgmSource == null)
            {
                bgmSource = GetComponent<AudioSource>();
            }

            if (objectiveRunner == null)
            {
                objectiveRunner = FindAnyObjectByType<ObjectiveRunner>();
            }
        }

        private void HandleScenarioCompleted(MediProfen.Data.ScenarioData scenario)
        {
            PlayCompleteBgm();
        }

        public void PlayCompleteBgm()
        {
            if (isCompleteBgmPlaying || bgmSource == null || completeBgm == null)
            {
                return;
            }

            isCompleteBgmPlaying = true;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (fadeDuration > 0f && gameObject.activeInHierarchy)
            {
                transitionRoutine = StartCoroutine(FadeToClip(completeBgm, completeVolume, false));
            }
            else
            {
                PlayClip(completeBgm, completeVolume, false);
            }
        }

        private void PlayClip(AudioClip clip, float volume, bool isLoop)
        {
            bgmSource.clip = clip;
            bgmSource.loop = isLoop;
            bgmSource.volume = volume;
            bgmSource.Play();
        }

        private IEnumerator FadeToClip(AudioClip clip, float targetVolume, bool isLoop)
        {
            float startVolume = bgmSource.volume;

            for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.clip = clip;
            bgmSource.loop = isLoop;
            bgmSource.Play();

            for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, time / fadeDuration);
                yield return null;
            }

            bgmSource.volume = targetVolume;
            transitionRoutine = null;
        }
    }
}
