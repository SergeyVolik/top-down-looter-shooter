using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SV.UI
{
    public class UINavigationManager : MonoBehaviour, INavigationManager
    {
        public static UINavigationManager Instance { get; private set; }
        public Stack<INavigateable> Navigateables => _navigateables;

        private Stack<INavigateable> _navigateables = new Stack<INavigateable>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            while (Navigateables.Count > 0)
            {
                Pop();
            }
        }

        public bool IsCurrentPage(INavigateable navigateable)
        {
            if (Navigateables.Count == 0)
                return false;

            if (navigateable == Navigateables.Peek())
                return true;

            return false;
        }
        public void Navigate(INavigateable navigateable, bool additive = false)
        {

          

            if (Navigateables.Count > 0)
            {
                if (Navigateables.Peek() == navigateable)
                {
                    Debug.LogError("Page already active");
                    return;
                }

                var prev = Navigateables.Pop();
                prev.Hide(additive);
                Navigateables.Push(prev);
            }

            navigateable.Show();
            Navigateables.Push(navigateable);
        }

        public INavigateable Pop()
        {
            INavigateable page = null;

            if (Navigateables.Count > 0)
            {
                page = Navigateables.Pop();

                page.Hide(false);

            }
            else {
                Debug.LogError("Navigateables are empty");
                return null;
            }

            if (Navigateables.Count > 0)
            {
                var prev = Navigateables.Pop();

                prev.Show();

                Navigateables.Push(prev);

            }

            return page;
        }
    }

    public interface INavigationManager
    {
        public Stack<INavigateable> Navigateables { get; }
        void Navigate(INavigateable navigateable, bool onlyDisableInput);
        INavigateable Pop();
        bool IsCurrentPage(INavigateable navigateable);
    }

    public interface INavigateable
    {

        void Show();

        void Hide(bool onlyDisableInput);
    }





}
