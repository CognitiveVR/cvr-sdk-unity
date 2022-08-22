using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CognitiveVR.ActiveSession
{
	public class DynamicSortable : MonoBehaviour
	{
		public int Sequence; //stored here in case list ever gets sorted to some different order
		public string Id;
		public string ObjectName;
		public int Visits;
		public long DurationMs;

		public Text NameText;
		public Text SequenceText;
		public Text DurationText;
		public Text VisitsText;

		public Image BackgroundImage;

		public void SetDynamic(string id, string objectName, long duration, int sequence, int visits)
        {
			Sequence = sequence;
			Id = id;
			ObjectName = objectName;
			Visits = visits;
			DurationMs += duration;
			SetDirty();
		}

		public void SetBackgroundColor(Color color)
		{
			BackgroundImage.color = color;
		}

		public void SetDirty()
        {
			NameText.text = ObjectName;
			SequenceText.text = Sequence.ToString();
			DurationText.text = DurationMs.ToString();
			VisitsText.text = Visits.ToString();
		}
	}
}