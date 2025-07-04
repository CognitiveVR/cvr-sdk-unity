using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//indicates the implementor can be 'focused'
//primarily used by controller pointer
//'MonoBehaviour' property allows accessing the monobehaviour from this interface

namespace Cognitive3D
{
    public interface IPointerFocus
    {
        /// <summary>
        /// Sets the focus state for the pointer when it hovers over the button.
        /// If the hover is true, the button will begin to slowly fill, and once the fill is complete, the button is considered selected.
        /// </summary>
        /// <param name="activation">Indicates if the button is being clicked (true) or not (false).</param>
        /// <param name="hover">Indicates whether the button should visually start filling (true for fill, false to stop the fill).</param>
        void SetPointerFocus(bool activation, bool hover);
        MonoBehaviour MonoBehaviour { get; }
    }
}