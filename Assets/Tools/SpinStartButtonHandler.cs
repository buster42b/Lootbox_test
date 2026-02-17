using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;
using UnityEngine.UI;

public class SpinStartButtonHandler : MonoBehaviourExtBind
{
    private Button _button;
    [SerializeField] private float targetSpinSpeed = 15;
    [SerializeField] private float accelerationTime = 1;

    [OnAwake]
    public void Init()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
        Model.Set("AccelerationTime", accelerationTime);
        
        void OnClick()
        {
            Model.EventManager.Invoke("StartSpin");
            Model.Set("Speed", targetSpinSpeed);
        }
    }
    
    [Bind("OnStartButtonEnabledChanged")]
    private void OnStartButtonEnabledChanged(bool enabled)
    {
        _button.interactable = enabled;
    }
}
