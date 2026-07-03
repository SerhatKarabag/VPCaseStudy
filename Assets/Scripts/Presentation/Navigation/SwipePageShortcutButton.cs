using UnityEngine;

namespace ThreadRace.Presentation.Navigation
{
    public sealed class SwipePageShortcutButton : MonoBehaviour
    {
        [SerializeField] private SwipePageController _pageController;
        [Min(0)]
        [SerializeField] private int _pageIndex;

        public static System.Action<int> OnPageOpened;

        public void OpenPage()
        {
            OnPageOpened?.Invoke(_pageIndex);

            if (_pageController == null)
            {
                Debug.LogWarning("[SwipePageShortcutButton] SwipePageController is not assigned.", this);
                return;
            }

            _pageController.GoToPage(_pageIndex);
        }
    }
}
