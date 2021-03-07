using Assets.Scripts.Managers;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class SaveLoadPicker : MonoBehaviour
    {
        public readonly string DefaultFilenameFormat = "WISM{0}.SAV";

        [SerializeField]
        private Button okButton;
        [SerializeField]
        private Button cancelButton;

        private UnityManager unityGame;
        private GameManager gameManager;
        private bool isInitialized;
        private string[] filenames;

        public int SelectedIndex { get; private set; }
        public OkCancel OkCancelResult { get; private set; }

        public void Initialize(UnityManager unityGame, bool isSaving)
        {
            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }            

            this.unityGame = unityGame;
            this.gameManager = unityGame.GameManager;

            RenderRows();

            this.gameObject.SetActive(true);
            this.okButton.interactable = false;
            this.okButton.GetComponentInChildren<Text>().text = (isSaving) ? "Save" : "Load";
            this.cancelButton.interactable = false;            
            this.OkCancelResult = OkCancel.Picking;

            this.isInitialized = true;
        }

        public bool IsInitialized()
        {
            return this.isInitialized;
        }

        public void SetCurrentItem(int index)
        {
            if (index < 0)
            {
                this.okButton.interactable = false;
                return;
            }

            SelectedIndex = index;            
            this.okButton.interactable = true;
        }

        public string GetCurrentFilename()
        {
            if (SelectedIndex < 0)
            {
                return null;
            }

            // TODO: Need to expand the array if we add a new save
          

            return this.filenames[SelectedIndex];
        }

        public string GetCurrentSaveName()
        {
            if (SelectedIndex < 0)
            {
                return null;
            }

            return transform.Find("Filename" + (SelectedIndex + 1))
                    .transform
                    .GetComponentInChildren<Text>()
                    .text;
        }

        public void Ok()
        {
            if (SelectedIndex < 0)
            {
                // Nothing to select
                Cancel();
                return;
            }

            this.OkCancelResult = OkCancel.Ok;
            Close();
        }

        public void Cancel()
        {
            this.SelectedIndex = -1;
            this.OkCancelResult = OkCancel.Cancel;
            Close();
        }

        private void Close()
        {
            this.isInitialized = false;
            this.gameObject.SetActive(false);
        }

        private void RenderRows()
        {
            filenames = GetSavedFileNames();
            for (int i = 0; i < filenames.Length; i++)
            {
                transform.Find("Filename" + (i + 1))
                    .transform
                    .GetComponentInChildren<Text>()
                    .text = Path.GetFileNameWithoutExtension(filenames[i]);

            }
        }

        private string[] GetSavedFileNames()
        {
            var path = Application.persistentDataPath;
            var pattern = String.Format(DefaultFilenameFormat, "?");
            return Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly);
        }

        public void Clear()
        {
            this.SelectedIndex = -1;
            this.OkCancelResult = OkCancel.None;
        }
    }
}
