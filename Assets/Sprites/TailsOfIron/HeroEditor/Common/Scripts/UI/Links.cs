using UnityEngine;

namespace Assets.HeroEditor.Common.Scripts.UI
{
    public class Links : MonoBehaviour
    {
        public void Navigate(string url)
        {
            Application.OpenURL(url);
        }
    }
}