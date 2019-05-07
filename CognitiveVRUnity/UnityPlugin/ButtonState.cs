using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonState
{
    public int ButtonPercent = 0;
    public float X = 0;
    public float Y = 0;
    public bool IncludeXY = false;

    public ButtonState(int buttonPercent, float x = 0, float y = 0, bool includexy = false)
    {
        ButtonPercent = buttonPercent;
        X = x;
        Y = y;
        IncludeXY = includexy;
    }

    public ButtonState(ButtonState source)
    {
        ButtonPercent = source.ButtonPercent;
        IncludeXY = source.IncludeXY;
        X = source.X;
        Y = source.Y;
    }

    //compare as if simply a container for data
    public override bool Equals(object obj)
    {
        var s = (ButtonState)obj;

        if (!IncludeXY)
        {
            return s.ButtonPercent == ButtonPercent;
        }
        else
        {
            return s.ButtonPercent == ButtonPercent && Mathf.Approximately(s.X, X) && Mathf.Approximately(s.Y, Y);
        }
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public void Copy(ButtonState source)
    {
        ButtonPercent = source.ButtonPercent;
        IncludeXY = source.IncludeXY;
        X = source.X;
        Y = source.Y;
    }
}