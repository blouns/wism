using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField]
        private string nextSceneName = "GameSetup";

        private RectTransform artwork;
        private RectTransform artworkParent;
        private Bounds artworkBounds;
        private Vector2 lastParentSize;

        public IEnumerator Start()
        {
            if (!this.showSplashOnStart)
            {
                NextScene();
                yield break;
            }

            InitializeArtworkLayout();
            Show();
            yield return new WaitForEndOfFrame();
            PlayAudio();
            yield return new WaitForSeconds(this.waitTime);
            Clear();
            NextScene();
        }

        private void InitializeArtworkLayout()
        {
            this.artwork = this.splashScreenGroup.transform as RectTransform;
            this.artworkParent = this.artwork?.parent as RectTransform;
            if (this.artworkParent == null) return;
            Canvas.ForceUpdateCanvases();
            // Fit the complete authored composition as one unit, including art
            // extending beyond the panel. Child proportions and offsets stay intact.
            this.artworkBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(this.artwork, this.artwork);
            this.artwork.anchorMin = this.artwork.anchorMax = new Vector2(.5f, .5f);
            FitArtwork();
        }

        private void LateUpdate()
        {
            if (this.artworkParent != null && this.artworkParent.rect.size != this.lastParentSize)
                FitArtwork();
        }

        private void FitArtwork()
        {
            var size = this.artworkParent.rect.size;
            if (size.x <= 0f || size.y <= 0f || this.artworkBounds.size.x <= 0f || this.artworkBounds.size.y <= 0f) return;
            var scale = Mathf.Min(size.x / this.artworkBounds.size.x, size.y / this.artworkBounds.size.y);
            this.artwork.localScale = Vector3.one * scale;
            this.artwork.anchoredPosition = -(Vector2)this.artworkBounds.center * scale;
            this.lastParentSize = size;
        }

        private void PlayAudio()
        {
            GetComponent<AudioSource>().Play();
        }

        private void NextScene()
        {
            if (!string.IsNullOrWhiteSpace(this.nextSceneName))
            {
                SceneManager.LoadScene(this.nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(this.nextScene);
            }
        }

        private void Show()
        {
            this.splashScreenGroup.alpha = 1f;
        }

        private void Clear()
        {
            this.splashScreenGroup.alpha = 0f;
            this.gameObject.SetActive(false);
        }
    }
}
