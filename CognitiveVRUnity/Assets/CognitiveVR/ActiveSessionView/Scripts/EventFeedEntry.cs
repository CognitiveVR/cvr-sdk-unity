using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CognitiveVR.ActiveSession
{
    public class EventFeedEntry : MonoBehaviour
    {
        public Text Title;
        public Text PropertyKeys;
        public Text PropertyValues;
        public Text ObjectText;
        public Text LocalTimeText;
        public Text SessionTimeText;
        public Image BackgroundImage;

        public RectTransform PropertyPanel;
        public RectTransform DetailPanel;

        [ContextMenu("calc")]
        public void CalcSize()
        {
            var rect = GetComponent<RectTransform>();
            float detailHeight = 10; //object id, local time, session time
            float propertyHeight = PropertyKeys.rectTransform.sizeDelta.y; //property details
            propertyHeight = Mathf.Max(propertyHeight, detailHeight);
            float titleHeight = 10;
            float titleSpace = 5;
            
            PropertyPanel.sizeDelta = new Vector2(PropertyPanel.sizeDelta.x, propertyHeight);
            DetailPanel.sizeDelta = new Vector2(DetailPanel.sizeDelta.x, (detailHeight * 3) + PropertyPanel.sizeDelta.y);
            
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, DetailPanel.sizeDelta.y + titleHeight + titleSpace);

            Vector3 panelLocalPosition = DetailPanel.localPosition;
            panelLocalPosition.y = -(titleHeight + titleSpace);
            DetailPanel.localPosition = panelLocalPosition;
        }

        public bool DirtySize = true;

        public void SetEvent(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time)
        {
            Title.text = name;

            if (properties == null || properties.Count == 0)
            {
                PropertyKeys.text = "none";
                PropertyValues.text = string.Empty;
            }
            else
            {
                PropertyKeys.text = string.Empty;
                PropertyValues.text = string.Empty;
                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyKeys.text += properties[i].Key + ":";
                    PropertyValues.text += properties[i].Value;
                    if (i != properties.Count - 1)
                    {
                        PropertyKeys.text += "\n";
                        PropertyValues.text += "\n";
                    }

                }
                //change panel sizes
            }

            if (string.IsNullOrEmpty(dynamicObjectId))
            {
                ObjectText.text = "none";
            }
            else
            {
                string dynamicname;
                if (CognitiveVR.DynamicManager.GetDynamicObjectName(dynamicObjectId, out dynamicname))
                {
                    ObjectText.text = dynamicname;
                }
                else
                {
                    ObjectText.text = dynamicObjectId;
                }
            }

            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(time).ToLocalTime();
            
            LocalTimeText.text = dtDateTime.ToLocalTime().ToString("HH:mm:ss");

            double sessionTimeSec = (time - CognitiveVR.Core.SessionTimeStamp);
            System.TimeSpan ts = new System.TimeSpan(0, 0, (int)sessionTimeSec);
            string prettySessionTime = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");

            SessionTimeText.text = prettySessionTime;
        }

        public void SetBackgroundColor(Color color)
        {
            BackgroundImage.color = color;
        }
    }
}