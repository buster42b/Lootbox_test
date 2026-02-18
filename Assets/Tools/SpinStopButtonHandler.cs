using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;
using UnityEngine.UI;

public class SpinStopButtonHandler : MonoBehaviourExtBind
{
    private Button _button;
    [SerializeField] private float decelerationTime = 5;

    [OnAwake]
    public void Init()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
        Model.Set("DecelerationTime", decelerationTime + Random.Range(-1f, 1f));
        
        void OnClick()
        {
            Model.EventManager.Invoke("StopSpin");
            Model.Set("Speed", 0);
        }
    }
    
    [Bind("OnStopButtonEnabledChanged")]
    private void OnStartButtonEnabledChanged(bool enabled)
    {
        _button.interactable = enabled;
    }
}
