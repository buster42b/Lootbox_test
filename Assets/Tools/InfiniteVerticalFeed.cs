using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AxGrid.Base;

namespace Tools
{
    public class InfiniteVerticalFeed : MonoBehaviourExt
    {
        public float currentVelocity;
        public RectTransform viewport;
        public RectTransform content => (RectTransform)layoutGroup.transform;
        public VerticalLayoutGroup layoutGroup;

        public RectTransform[] itemList;

        private float OldVelocity;
        private bool isUpdated;
        
        [OnStart]
        private void Initiate()
        {
            isUpdated = false;
            OldVelocity = 0;
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

        [OnUpdate]
        private void Move()
        {
            content.localPosition += new Vector3(0, currentVelocity);
            
            if (isUpdated)
            {
                isUpdated = false;
                currentVelocity = OldVelocity;
            }
            
            if (content.localPosition.y > 0)
            {
                Canvas.ForceUpdateCanvases(); 
                OldVelocity = currentVelocity;
                content.localPosition -=
                    new Vector3(0, itemList.Length * (itemList[0].rect.height + layoutGroup.spacing), 0);
                isUpdated = true;

            }

            if (content.localPosition.y < 0- itemList.Length* (itemList[0].rect.height + layoutGroup.spacing))
            {
                Canvas.ForceUpdateCanvases();
                OldVelocity = currentVelocity;
                content.localPosition +=
                    new Vector3(0, itemList.Length * (itemList[0].rect.height + layoutGroup.spacing), 0);
                    isUpdated = true;
            }
        }
    }
}
