using Assets.Scripts.Managers;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private bool isInitialized;
        private string[] filenames;
        private bool isSaving;

        public int SelectedIndex { get; private set; }
        public OkCancel OkCancelResult { get; private set; }

        public void Initialize(UnityManager unityGame, bool isSaving)
        {
            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            this.gameObject.SetActive(true);
            this.okButton.interactable = false;
            this.cancelButton.interactable = false;
            this.OkCancelResult = OkCancel.Picking;
            this.isSaving = isSaving;

            if (this.isSaving)
            {
                this.okButton.GetComponentInChildren<Text>().text = "Save";
            }
            else
            {
                this.okButton.GetComponentInChildren<Text>().text = "Load";
            }

            this.filenames = GetSavedFileNames();
            RenderRows();

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
                    .GetComponentInChildren<InputField>()
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
        
        public void OnFieldSelect(int index)
        {
            SetCurrentItem(index);
            var filenameInput = GetFilenameInputbox(index);

            if (isSaving)
            {
                // Select the input field for this row
                EventSystem.current.SetSelectedGameObject(filenameInput.gameObject);
            }
        }

        private Transform GetFilenameInputbox(int index)
        {
            return transform.Find("Filename" + (index + 1));
        }

        private void Close()
        {
            this.isInitialized = false;
            this.gameObject.SetActive(false);
        }

        private void RenderRows()
        {            
            for (int i = 0; i < 8; i++)
            {
                var rowButton = GetRowButton(i);
                var filenameInput = GetFilenameInputbox(i);
                var inputField = filenameInput.GetComponent<InputField>();

                inputField.enabled = isSaving;
                rowButton.interactable = isSaving;

                if (i < filenames.Length)
                {
                    // Existing file for this slot
                    SetSaveName(i, inputField);

                    // Allow load only on existing saves
                    rowButton.interactable = true;
                }
                else
                {
                    // Clear slot as there is no existing file save
                    inputField.SendMessage("Reset");
                    rowButton.interactable = false;
                }
            }            
        }

        private Button GetRowButton(int index)
        {
            var button = transform.Find("Button" + (index + 1));
            if (button == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Row not found.");
            }

            return button.GetComponent<Button>();
        }

        private void SetSaveName(int i, InputField inputField)
        {
            string saveName = "WISM + " + (i + 1);

            try
            {
                saveName = GetSaveNameFromFile(filenames[i]);
            }
            catch (Exception ex)
            {
                // Swallow and use default as there might be some change to format
                // handled elsewhere. 
                Debug.LogError(ex.Message);
            }

            inputField.text = saveName;
        }

        private string GetSaveNameFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            // Save is a JSON which starts with the Display name;
            // so for perf, grab the first row and parse it out.
            // e.g. '{"DisplayName":"WISM1","WorldName":"Illuria",...'
            foreach (var line in File.ReadLines(path))
            {
                string displayToken = "\"DisplayName\":";
                int tokenIndex = line.IndexOf(displayToken);
                if (tokenIndex >= 0)
                {
                    int index = tokenIndex + displayToken.Length;
                    var displayName = new StringBuilder();
                    if (line[index] != '"')
                    {
                        throw new FileLoadException("File could not be loaded", path);
                    }

                    // Parse DisplayName value
                    while (line[++index] != '"')
                    {
                        displayName.Append(line[index]);
                    }

                    return displayName.ToString();
                }
            }

            throw new FileLoadException("File could not be loaded", path);
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
