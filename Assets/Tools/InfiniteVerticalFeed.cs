using System;
using AxGrid;
using UnityEngine;
using UnityEngine.UI;
using AxGrid.Base;
using AxGrid.FSM;
using AxGrid.Model;
using AxGrid.Path;

namespace Tools
{
    public class InfiniteVerticalFeed : MonoBehaviourExtBind
    {
        private float targetSpinSpeed;
        [SerializeField] private float minimalStoppingSpeed;
        [SerializeField] private ParticleSystem particles;
        public RectTransform viewport;
        public RectTransform content => (RectTransform)layoutGroup.transform;
        public VerticalLayoutGroup layoutGroup;
        public RectTransform[] itemList;

        private float oldVelocity;
        private bool isUpdated;
        private RectTransform selectedItem;
        
        [OnAwake]
        private void CreateFsm()
        {
            Settings.Fsm = new FSM();
            Settings.Fsm.Add(new IsIdle());
            Settings.Fsm.Add(new IsAccelerating());
            Settings.Fsm.Add(new IsSpinning());
            Settings.Fsm.Add(new IsDecelerating());
        }
        
        [OnUpdate]
        public void UpdateFsm()
        {
            Settings.Fsm.Update(Time.deltaTime);
        }
        
        [OnStart]
        private void Initiate()
        {
            particles.Stop();
            Settings.Fsm.Start("IsIdle");
            Model.Set("SpinningSpeed", 0f);
            Model.Set("StartButtonEnabled", true);
            Model.Set("StopButtonEnabled", false);
            
            Model.EventManager.AddAction("TargetSpeedReached", OnTargetSpeedReached);
            
            isUpdated = false;
            oldVelocity = 0;
            int itemsToAdd = Mathf.CeilToInt(viewport.rect.height / (itemList[0].rect.height + layoutGroup.spacing));

            for (int i = 0; i < itemsToAdd; i++)
            {
                RectTransform rt = Instantiate(itemList[i % itemList.Length], content);
                rt.SetAsLastSibling();
            }
            for (int i = 0; i < itemsToAdd; i++)
            {
                int num = itemList.Length - i - 1;

                while (num<0)
                {
                    num += itemList.Length;
                }
                
                RectTransform rt = Instantiate(itemList[num], content);
                rt.SetAsFirstSibling();
            }

            content.localPosition = new Vector3(
                content.localPosition.x,
                0 - (itemList[0].rect.height + layoutGroup.spacing) * itemsToAdd,
                content.localPosition.z);
        }
        
        private void OnTargetSpeedReached()
        {
            if (Settings.Fsm.CurrentStateName == "IsAccelerating")
            {
                Settings.Fsm.Change("IsSpinning");
            }
            else if (Settings.Fsm.CurrentStateName == "IsDecelerating")
            {
                Settings.Fsm.Change("IsIdle");
            }
        }
        
        [Bind("OnSpeedChanged")]
        public void OnSpeedChanged(float value)
        {
            var accelerationTime = Settings.Fsm.CurrentStateName == "IsAccelerating"
                ? Model.Get<float>("AccelerationTime")
                : Model.Get<float>("DecelerationTime");
            this.Path = new CPath()
                .EasingLinear(accelerationTime, targetSpinSpeed, value, (f) => {
                    targetSpinSpeed = f;
                    Model.Set("SpinningSpeed", f);
                    selectedItem = GetClosestItemToViewportCenter();
                    if (f <= minimalStoppingSpeed && Vector2.Distance(viewport.pivot, selectedItem.pivot) < .01f)
                        targetSpinSpeed = 0;
                })
                .EasingLinear(.5f,0,.5f, (f) =>
                {
                    if (Settings.Fsm.CurrentStateName != "IsDecelerating" || 
                        !(targetSpinSpeed <= minimalStoppingSpeed)) 
                        return;
                    RectTransform closestItem = GetClosestItemToViewportCenter();
                    if (closestItem == null) return;
                    
                    Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
                    Vector3 itemCenterWorld = closestItem.TransformPoint(closestItem.rect.center);
                    float verticalDistance = viewportCenterWorld.y - itemCenterWorld.y;
                            
                    bool isPerfectlyAligned = Mathf.Abs(verticalDistance) < 0.01f;
                    Debug.Log($"Closest item: {closestItem.name}, Vertical distance: {verticalDistance:F3}, Perfectly aligned: {isPerfectlyAligned}");
                            
                    if (!isPerfectlyAligned)
                        content.localPosition += new Vector3(0, verticalDistance * f);
                    else
                    {
                        targetSpinSpeed = 0f;
                        Model.Set("SpinningSpeed", 0f);
                    }
                })
                .Action(() =>
                {
                    if (Settings.Fsm.CurrentStateName == "IsDecelerating") PrizeVFX();
                })
                .Wait(1)
                .Action(() => {
                    Settings.Invoke("TargetSpeedReached");
                });
        }
        
        [OnUpdate]
        private void Move()
        {
            content.localPosition += new Vector3(0, targetSpinSpeed);
            
            if (isUpdated)
            {
                isUpdated = false;
                targetSpinSpeed = oldVelocity;
            }
            
            if (content.localPosition.y > 0)
            {
                Canvas.ForceUpdateCanvases(); 
                oldVelocity = targetSpinSpeed;
                content.localPosition -=
                    new Vector3(0, itemList.Length * (itemList[0].rect.height + layoutGroup.spacing), 0);
                isUpdated = true;

            }

            if (!(content.localPosition.y <
                  0 - itemList.Length * (itemList[0].rect.height + layoutGroup.spacing))) return;
            Canvas.ForceUpdateCanvases();
            oldVelocity = targetSpinSpeed;
            content.localPosition +=
                new Vector3(0, itemList.Length * (itemList[0].rect.height + layoutGroup.spacing), 0);
            isUpdated = true;
        }

        private void PrizeVFX()
        {
            if (selectedItem.TryGetComponent(out Image image))
            {
                var mainModule = particles.main;
                mainModule.startColor = image.color;
            }
            particles.Play();
        }
        
        private RectTransform GetClosestItemToViewportCenter()
        {
            if (content.childCount == 0)
                return null;
            
            RectTransform closestItem = null;
            float minDistance = float.MaxValue;
            
            Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
            
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform item = content.GetChild(i) as RectTransform;
                if (item == null) continue;
                
                Vector3 itemCenterWorld = item.TransformPoint(item.rect.center);
                float distance = Vector3.Distance(viewportCenterWorld, itemCenterWorld);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestItem = item;
                }
            }
            
            return closestItem;
        }
    }
    

    [State("IsSpinning")]
    public class IsSpinning : FSMState
    {
        [Enter]
        public void Enter()
        {
            Debug.Log("Enter IsSpinning");
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", true);
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit IsSpinning");
        }
    }

    [State("IsIdle")]
    public class IsIdle: FSMState
    {
        [Enter]
        public void Enter()
        {
            Debug.Log("Enter IsIdle");
            Model.Set("SpinningSpeed", 0f);
            Model.Set("StartButtonEnabled", true);
            Model.Set("StopButtonEnabled", false);
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit IsIdle");
        }
    }

    [State("IsAccelerating")]
    public class IsAccelerating : FSMState
    {
        [Enter]
        public void Enter()
        {
            Debug.Log("Enter IsAccelerating");
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", false);
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit IsAccelerating");
        }
    }
    
    [State("IsDecelerating")]
    public class IsDecelerating : FSMState
    {
        [Enter]
        public void Enter()
        {
            Debug.Log("Enter IsDecelerating");
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", false);
        }

        [Exit]
        public void Exit()
        {
            Debug.Log("Exit IsDecelerating");
        }
    }
}
