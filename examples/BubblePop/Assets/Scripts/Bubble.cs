using UnityEngine;
using System.Collections;

public class Bubble : MonoBehaviour 
{
	public bool HasPrize = false;
	
	//
	// Pop when you get clicked & tell your parent what happened
	//
	void OnMouseOver()
	{
		if(Input.GetMouseButtonDown(0))
		{
			SendMessageUpwards("OnPop", HasPrize);
			
			// pop!
			Destroy(gameObject);
		}
	}
	
	//
	// Pop when you're told
	//
	void OnPopAll()
	{
		Destroy(gameObject);
	}
}
