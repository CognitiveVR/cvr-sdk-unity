using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//SetPointerFocus called from ControllerPointer, or SetGazeFocus from HMDPointer
//fills an buttonImage over time
//activates OnFill action after pointing at this button for a duration

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/Virtual Button")]
    public class VirtualButton : MonoBehaviour, IPointerFocus
    {
        /// <summary>
        /// By default, this is set to false for the confirm button and becomes enabled after an answer is selected.
        /// </summary>
        [Tooltip("Set to true to allow the button to be clickable.")]
        public bool isEnabled = true;

        /// <summary>
        /// The image component that fills up when the button is focused, if slowFill is enabled.
        /// </summary>
        [Tooltip("The image that will visually fill when the button is focused, applicable only if slowFill is enabled.")]
        [SerializeField]
        protected Image fillImage;

        /// <summary>
        /// The image component for the button's UI. <br/>
        /// Used to update the button's color.
        /// </summary>
        [Tooltip("The image used for the button's UI. Allows updating the button's color.")]
        [SerializeField]
        protected Image buttonImage;

        /// <summary>
        /// Duration in seconds for the button to fill before confirming the action. 
        /// </summary>
        [Tooltip("Time in seconds for the button to fill before invoking the confirm action.")]
        [SerializeField]
        protected float FillDuration = 1;

        /// <summary>
        /// The default color of the button <br/>
        /// Grey
        /// </summary>
        private Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        /// <summary>
        /// The color to set button to when "selected" <br/>
        /// Green
        /// </summary>
        private Color selectedColor = new Color(0, 1, 0.05f, 1);

        /// <summary>
        /// Event or function to be executed when the button is clicked and confirmed.
        /// </summary>
        [Tooltip("Event/function triggered when the button is clicked and the fill is completed.")]
        [UnityEngine.Serialization.FormerlySerializedAs("OnFill")]
        [SerializeField]
        protected UnityEngine.Events.UnityEvent OnConfirm;

        /// <summary>
        /// Set to true if you want the buttons to resize dynamically.
        /// </summary>
        [Header("Resize Settings")]
        [Tooltip("Enable this to allow the button to resize dynamically.")]
        [SerializeField]
        private bool dynamicallyResize;

        /// <summary>
        /// A reference to the collider for this button <br/>
        /// We need this to adjust collisions while resizing buttons <br/>
        /// Consider using GetComponent instead of keeping as a public var
        /// </summary>
        [Tooltip("Collider reference used for adjusting button collision during resizing.")]
        [SerializeField]
        private BoxCollider boxCollider;

        /// <summary>
        /// A reference to the rect for this button <br/>
        /// We need this to adjust the UI while resizing buttons <br/>
        /// Consider using GetComponent instead of keeping as a public var
        /// </summary>
        [Tooltip("RectTransform reference used for adjusting button size and position during resizing.")]
        [SerializeField]
        private RectTransform rectTransform;

        /// <summary>
        /// Float value representing how much the button has "filled"
        /// </summary>
        protected float FillAmount;

        /// <summary>
        /// True if the button has been focused on this frame <br/>
        /// Used to invoke click or fill
        /// </summary>
        protected bool focusThisFrame = false;

        /// <summary>
        /// True if button can be interacted with; false otherwise
        /// </summary>
        protected bool canActivate = true;

        /// <summary>
        /// True if button should fill to activate (like in HMDPointer) <br/>
        /// Set to true if hands; false if controller <br/>
        /// Passed in from ControllerPointer
        /// </summary>
        protected bool slowFill = true;

        /// <summary>
        /// Saves the color the fill image starts with
        /// </summary>
        protected Color fillStartingColor;

        /// <summary>
        /// The color to set on the confirm button when it is enabled <br/>
        /// A light blue
        /// </summary>
        private readonly Color confirmColor = new Color(0.12f, 0.64f, 0.96f, 1f);

        /// <summary>
        /// True if the button is "selected"
        /// </summary>
        private bool isSelected;

        public MonoBehaviour MonoBehaviour { get { return this; } }

        /// <summary>
        /// Saves the fill starting color
        /// </summary>
        protected virtual void Start()
        {
            if (buttonImage != null)
            {
                defaultColor = buttonImage.color;
            }

            if (fillImage != null)
            {
                selectedColor = fillImage.color;
                fillStartingColor = fillImage.color;
            }

            // We wait one frame because:
            // Rects driven by layout don't have a value in Start()
            StartCoroutine(WaitOneFrame());
        }

        /// <summary>
        /// Wait for a frame and then dynamically resizes buttons <br/>
        /// We wait because: Rects driven by layout don't have a value in Start()
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitOneFrame()
        {
            yield return new WaitForEndOfFrame();
            if (dynamicallyResize)
            {
                DynamicallyResize();
            }
        }

        /// <summary>
        /// Called from the ControllerPointer script <br/>
        /// Enables select/click on this button
        /// </summary>
        /// <param name="activation">True if this button is being clicked</param>
        /// <param name="fill">True if the button will fill (like gaze button); false if otherwise</param>
        public virtual void SetPointerFocus(bool activation, bool fill)
        {
            if (canActivate == false)
            {
                return;
            }
            focusThisFrame = activation;
            slowFill = fill;
        }

        /// <summary>
        /// This is called from the HMDPointer script
        /// </summary>
        public virtual void SetGazeFocus()
        {
            if (canActivate == false)
            {
                return;
            }
            focusThisFrame = true;
        }

        /// <summary>
        /// Selects button if slowFill is false <br/>
        /// Otherwise gradually increase/decrease fill amount depending on focused/unfocused <br/>
        /// Invoke click if past threshold
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (isSelected) { return; }
            if (!isEnabled) { return; }

            if (!gameObject.activeInHierarchy) { return; }

            // Button interactable and focused
            if (canActivate && focusThisFrame)
            {
                // Immediately "click": usually used by controller
                if (!slowFill)
                {
                    StartCoroutine(FilledEvent());
                    OnConfirm.Invoke();
                }
                else // Increment the gradual fill: usually used by hand and hmd gaze
                {
                    FillAmount += Time.deltaTime;
                    // Fill complete, thus "click"
                    if (FillAmount > FillDuration)
                    {
                        canActivate = false;
                        StartCoroutine(FilledEvent());
                        OnConfirm.Invoke();
                    }
                }
            }

            // Make it interactable again
            if (!canActivate && FillAmount <= 0f && slowFill)
            {
                canActivate = true; 
            }


            // Button interactable and not focused: unfill the fill
            if (!focusThisFrame && canActivate)
            {
                FillAmount -= Time.deltaTime;
                FillAmount = Mathf.Clamp(FillAmount, 0, FillDuration);
            }

            // Update the button image
            if (fillImage != null && slowFill)
            {
                fillImage.fillAmount = FillAmount / FillDuration;
            }

            focusThisFrame = false;
        }

        /// <summary>
        /// When filled, change the color of the fill ring to black for half a second before returning to starting color
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator FilledEvent()
        {
            float t = 0;
            float duration = 0.5f;
            while (t < 1)
            {
                yield return null;
                t += Time.deltaTime / duration;
                if (fillImage != null)
                    fillImage.color = Color.black;
            }
            FillAmount = 0;
            yield return null;
            if (fillImage != null)
                fillImage.color = fillStartingColor;
        }

        /// <summary>
        /// Sets the buttons "selection" state
        /// </summary>
        /// <param name="select">True if button selected; false otherwise</param>
        public void SetSelect(bool select)
        {
            isSelected = select;
            buttonImage.color = select ? selectedColor : defaultColor;
        }

        /// <summary>
        /// Enables the confirm button <br/>
        /// It is disabled if no option is chosen
        /// </summary>
        public void SetConfirmEnabled()
        {
            buttonImage.color = confirmColor;
            isEnabled = true;
        }

        /// <summary>
        /// Resizes the button(s) <br/>
        /// Can be used in scale answers where there are variable number of options
        /// </summary>
        private void DynamicallyResize()
        {
            var rect = rectTransform.rect;
            boxCollider.size = rect.size;
        }
    }
}