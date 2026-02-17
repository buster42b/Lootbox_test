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
            Settings.Fsm.Add(new IdleState());
            Settings.Fsm.Add(new AcceleratingState());
            Settings.Fsm.Add(new SpinningState());
            Settings.Fsm.Add(new DeceleratingState());
            
            // Set up event listeners for FSM transitions
            Model.EventManager.AddAction("StartSpin", () => Settings.Fsm.Change("Accelerating"));
            Model.EventManager.AddAction("StopSpin", () => Settings.Fsm.Change("Decelerating"));
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
            Settings.Fsm.Start("Idle");
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
            string currentState = Settings.Fsm.CurrentStateName;
            if (currentState == "Accelerating")
            {
                Settings.Fsm.Change("Spinning");
            }
            else if (currentState == "Decelerating")
            {
                Settings.Fsm.Change("Idle");
            }
        }
        
        [Bind("OnSpeedChanged")]
        public void OnSpeedChanged(float value)
        {
            var accelerationTime = Settings.Fsm.CurrentStateName == "Accelerating"
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
                .EasingLinear(2,0,1, (f) =>
                {
                    if (Settings.Fsm.CurrentStateName != "Decelerating" || 
                        !(targetSpinSpeed <= minimalStoppingSpeed)) 
                        return;
                    RectTransform closestItem = GetClosestItemToViewportCenter();
                    if (closestItem == null) return;
                    
                    Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
                    Vector3 itemCenterWorld = closestItem.TransformPoint(closestItem.rect.center);
                    float verticalDistance = viewportCenterWorld.y - itemCenterWorld.y;
                            
                    bool isPerfectlyAligned = Mathf.Abs(verticalDistance) < 0.01f;
                            
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
                    if (Settings.Fsm.CurrentStateName == "Decelerating") PrizeVFX();
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
    

    [State("Spinning")]
    public class SpinningState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", true);
        }
    }

    [State("Idle")]
    public class IdleState: FSMState
    {
        [Enter]
        public void Enter()
        {
            Model.Set("SpinningSpeed", 0f);
            Model.Set("StartButtonEnabled", true);
            Model.Set("StopButtonEnabled", false);
        }
    }

    [State("Accelerating")]
    public class AcceleratingState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", false);
        }
    }
    
    [State("Decelerating")]
    public class DeceleratingState : FSMState
    {
        [Enter]
        public void Enter()
        {
            Model.Set("StartButtonEnabled", false);
            Model.Set("StopButtonEnabled", false);
        }
    }
}
