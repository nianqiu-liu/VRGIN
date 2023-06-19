using UnityEngine;

namespace VRGIN.Controls
{
    public class RightController : Controller
    {
        public static RightController Create()
        {
            return new GameObject("Right Controller").AddComponent<RightController>();
        }
    }
}
