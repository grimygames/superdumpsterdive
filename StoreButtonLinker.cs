using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StoreButtonLinker : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Clear inspector listeners and bind directly to the persistent Singleton instance
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (DumpsterStoreUI.Instance != null)
                {
                    DumpsterStoreUI.Instance.TogglePauseStateEngine();
                }
                else
                {
                    Debug.LogError("[StoreButtonLinker] DumpsterStoreUI.Instance is missing!");
                }
            });
        }
    }
}