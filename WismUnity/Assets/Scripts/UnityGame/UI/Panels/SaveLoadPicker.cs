using Assets.Scripts.Managers;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
            this.cancelButton.interactable = true;
            this.SelectedIndex = -1;
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
            if (index < 0 || this.filenames == null || index >= this.filenames.Length ||
                (!this.isSaving && !File.Exists(this.filenames[index])))
            {
                this.SelectedIndex = -1;
                this.okButton.interactable = false;
                return;
            }

            this.SelectedIndex = index;
            this.okButton.interactable = true;
        }

        public string GetCurrentFilename()
        {
            if (this.SelectedIndex < 0)
            {
                return null;
            }

            return this.filenames[this.SelectedIndex];
        }

        public string GetCurrentSaveName()
        {
            if (this.SelectedIndex < 0)
            {
                return null;
            }

            return this.transform.Find("Filename" + (this.SelectedIndex + 1))
                    .transform
                    .GetComponentInChildren<InputField>()
                    .text;
        }

        public void Ok()
        {
            if (this.SelectedIndex < 0)
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

            if (this.isSaving)
            {
                // Select the input field for this row
                EventSystem.current.SetSelectedGameObject(filenameInput.gameObject);
            }
        }

        private Transform GetFilenameInputbox(int index)
        {
            return this.transform.Find("Filename" + (index + 1));
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

                inputField.enabled = this.isSaving;
                rowButton.interactable = this.isSaving;

                if (File.Exists(this.filenames[i]))
                {
                    // Existing file for this slot
                    SetSaveName(i, inputField);

                    // Allow load only on existing saves
                    rowButton.interactable = true;
                }
                else
                {
                    // Clear slot as there is no existing file save
                    inputField.text = this.isSaving ? "WISM " + (i + 1) : string.Empty;
                    rowButton.interactable = this.isSaving;
                }
            }
        }

        private Button GetRowButton(int index)
        {
            var button = this.transform.Find("Button" + (index + 1));
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
                saveName = GetSaveNameFromFile(this.filenames[i]);
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

            // Stream only the top-level display name, preserving JSON escapes
            // without deserializing the complete world for every slot.
            using (var stream = File.OpenText(path))
            using (var reader = new JsonTextReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && reader.Depth == 1 &&
                        string.Equals(reader.Value as string, "DisplayName", StringComparison.Ordinal) &&
                        reader.Read() && reader.TokenType == JsonToken.String)
                        return (string)reader.Value;
                }
            }

            throw new FileLoadException("File could not be loaded", path);
        }

        private string[] GetSavedFileNames()
        {
            return Enumerable.Range(1, 8)
                .Select(slot => Path.Combine(PersistanceManager.SaveDirectory, String.Format(this.DefaultFilenameFormat, slot)))
                .ToArray();
        }

        public void Clear()
        {
            this.SelectedIndex = -1;
            this.OkCancelResult = OkCancel.None;
        }
    }
}
