using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveCodeUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField saveCodeInputField;
    public Button exportButton;
    public Button importButton;
    public TMP_Text feedbackText;

    void Start()
    {
        if (exportButton != null) exportButton.onClick.AddListener(OnExportClicked);
        if (importButton != null) importButton.onClick.AddListener(OnImportClicked);
    }

    private void OnExportClicked()
    {
        if (PlayFabAuthBridge.Instance == null) return;

        string code = PlayFabAuthBridge.Instance.ExportSaveCode();
        if (saveCodeInputField != null)
        {
            saveCodeInputField.text = code;
        }

        // Copy to system clipboard
        GUIUtility.systemCopyBuffer = code;
        SetFeedback("Save code copied to clipboard!", Color.green);
    }

    private void OnImportClicked()
    {
        if (PlayFabAuthBridge.Instance == null || saveCodeInputField == null) return;

        string code = saveCodeInputField.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            SetFeedback("Please paste a save code first.", Color.red);
            return;
        }

        bool success = PlayFabAuthBridge.Instance.ImportSaveCode(code);
        if (success)
        {
            SetFeedback("Save loaded successfully!", Color.green);
        }
        else
        {
            SetFeedback("Invalid or corrupt save code.", Color.red);
        }
    }

    private void SetFeedback(string msg, Color col)
    {
        if (feedbackText != null)
        {
            feedbackText.text = msg;
            feedbackText.color = col;
        }
    }
}