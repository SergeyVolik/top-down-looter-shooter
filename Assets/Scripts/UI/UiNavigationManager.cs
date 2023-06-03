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
  
        public void Navigate(INavigateable navigateable, bool onlyDisableInput = false)
        {
            if (Navigateables.Count > 0)
            {
                var prev = Navigateables.Pop();
                prev.Hide(onlyDisableInput);
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

            return page;
        }
    }

    public interface INavigationManager
    {
        public Stack<INavigateable> Navigateables { get; }
        void Navigate(INavigateable navigateable, bool onlyDisableInput);
        INavigateable Pop();
    }

    public interface INavigateable
    {
        void Show();

        void Hide(bool onlyDisableInput);
    }



  

}
