using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Witchslayer.UI.Controls.Tabs
{

    public class TabButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Components")]
        public Button Button;
        public TMP_Text Text;

        [Header("Events")]
        public UnityEvent<PointerEventData> OnRightClick;

        /// <summary>
        /// Removes all listeners from the Button and OnRightClick events.
        /// </summary>
        public void RemoveAllListeners()
        {
            Button.onClick.RemoveAllListeners();
            OnRightClick.RemoveAllListeners();
        }

        private void OnValidate()
        {
            if (Button == null)
                Button = GetComponent<Button>();
            if (Text == null)
                Text = GetComponentInChildren<TMP_Text>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    break;
                case PointerEventData.InputButton.Right:
                    OnRightClick?.Invoke(eventData);
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }

    }

}
