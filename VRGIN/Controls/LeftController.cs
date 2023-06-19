using UnityEngine;

namespace VRGIN.Controls
{
    public class LeftController : Controller
    {
        public static LeftController Create()
        {
            return new GameObject("Left Controller").AddComponent<LeftController>();
        }
    }
}
