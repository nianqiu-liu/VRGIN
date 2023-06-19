using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;

namespace VRGIN.U46.Helpers
{
    public class GuiScaler
    {
        private GUIQuad _Gui;

        private Vector3? _StartLeft;

        private Vector3? _StartRight;

        private Vector3? _StartScale;

        private Quaternion? _StartRotation;

        private Vector3? _StartPosition;

        private Quaternion _StartRotationController;

        private Vector3? _OffsetFromCenter;

        private Transform _Left;

        private Transform _Right;

        private Vector3 TopLeft => _Left.position;

        private Vector3 BottomRight => _Right.position;

        private Vector3 Center => Vector3.Lerp(TopLeft, BottomRight, 0.5f);

        private Vector3 Up => Vector3.Lerp((VR.Camera.Head.position - TopLeft).normalized, (VR.Camera.Head.position - BottomRight).normalized, 0.5f);

        public GuiScaler(GUIQuad gui, Transform left, Transform right)
        {
            _Gui = gui;
            _Left = left;
            _Right = right;
            _StartLeft = left.position;
            _StartRight = right.position;
            _StartScale = _Gui.transform.localScale;
            _StartRotation = _Gui.transform.localRotation;
            _StartPosition = _Gui.transform.position;
            _StartRotationController = GetAverageRotation();
            Vector3.Distance(_StartLeft.Value, _StartRight.Value);
            var vector = _StartRight.Value - _StartLeft.Value;
            var vector2 = _StartLeft.Value + vector * 0.5f;
            _OffsetFromCenter = _Gui.transform.position - vector2;
        }

        public void Update()
        {
            if ((bool)_Left && (bool)_Right)
            {
                var num = Vector3.Distance(_Left.position, _Right.position);
                var num2 = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
                var vector = _Right.position - _Left.position;
                var vector2 = _Left.position + vector * 0.5f;
                var quaternion = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
                var averageRotation = GetAverageRotation();
                var quaternion2 = quaternion * averageRotation * Quaternion.Inverse(quaternion * _StartRotationController);
                _Gui.transform.localScale = num / num2 * _StartScale.Value;
                _Gui.transform.localRotation = quaternion2 * _StartRotation.Value;
                _Gui.transform.position = vector2 + averageRotation * Quaternion.Inverse(_StartRotationController) * _OffsetFromCenter.Value;
            }
        }

        private Quaternion GetAverageRotation()
        {
            var normalized = (_Right.position - _Left.position).normalized;
            var vector = Vector3.Lerp(_Left.forward, _Right.forward, 0.5f);
            return Quaternion.LookRotation(Vector3.Cross(normalized, vector).normalized, vector);
        }
    }
}
