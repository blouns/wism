using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace Assets.Scripts.UI.Panels
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(CanvasGroup))]
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField]
        private bool showSplashOnStart = true;
        [SerializeField]
        private float waitTime;
        [SerializeField]
        private CanvasGroup splashScreenGroup;
        [SerializeField]
        private int nextScene;

        public IEnumerator Start()
        {
            if (!this.showSplashOnStart)
            {
                NextScene();
            }
            
            Show();
            yield return new WaitForEndOfFrame();
            PlayAudio();
            yield return new WaitForSeconds(waitTime);
            Clear();
            NextScene();
        }

        private void PlayAudio()
        {
            GetComponent<AudioSource>().Play();
        }

        private void NextScene()
        {            
            SceneManager.LoadScene(nextScene);
            SceneManager.UnloadSceneAsync(0);
        }

        private void Show()
        {
            this.splashScreenGroup.alpha = 1f;
        }

        private void Clear()
        {
            this.splashScreenGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
