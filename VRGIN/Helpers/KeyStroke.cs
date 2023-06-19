using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRGIN.Helpers
{
    public class KeyStroke
    {
        private List<KeyCode> modifiers = new List<KeyCode>();

        private List<KeyCode> keys = new List<KeyCode>();

        private KeyCode[] MODIFIER_LIST = new KeyCode[6]
        {
            KeyCode.LeftAlt,
            KeyCode.RightAlt,
            KeyCode.LeftControl,
            KeyCode.RightControl,
            KeyCode.LeftShift,
            KeyCode.RightShift
        };

        public KeyStroke(string strokeString)
        {
            var array = (from key in strokeString.ToUpper().Split('+', '-')
                         select key.Trim()).ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                var text = array[i];
                switch (text)
                {
                    case "CTRL":
                        AddStroke(KeyCode.LeftControl);
                        continue;
                    case "ALT":
                        AddStroke(KeyCode.LeftAlt);
                        continue;
                    case "SHIFT":
                        AddStroke(KeyCode.LeftShift);
                        continue;
                }

                try
                {
                    if (Regex.IsMatch(text, "^\\d$")) text = "Alpha" + text;
                    if (Regex.IsMatch(text, "^(LEFT|RIGHT|UP|DOWN)$")) text += "ARROW";
                    AddStroke((KeyCode)Enum.Parse(typeof(KeyCode), text, true));
                }
                catch (Exception)
                {
                    Console.WriteLine("FAILED TO PARSE KEY \"{0}\"", text);
                }
            }

            Init();
        }

        public KeyStroke(IEnumerable<KeyCode> strokes)
        {
            foreach (var stroke in strokes) AddStroke(stroke);
            Init();
        }

        private void Init()
        {
            if (modifiers.Count > 0 && keys.Count == 0)
            {
                keys.AddRange(modifiers);
                modifiers.Clear();
            }
        }

        private void AddStroke(KeyCode stroke)
        {
            if (MODIFIER_LIST.Contains(stroke))
                modifiers.Add(stroke);
            else
                keys.Add(stroke);
        }

        public bool Check(KeyMode mode = KeyMode.PressDown)
        {
            if (modifiers.Count == 0 && keys.Count == 0) return false;
            if (modifiers.All((KeyCode key) => Input.GetKey(key)) && keys.All((KeyCode key) => mode != KeyMode.Press ? mode != 0 ? Input.GetKeyUp(key) : Input.GetKeyDown(key) : Input.GetKey(key)))
                return MODIFIER_LIST.Except(modifiers).All((KeyCode invalidModifier) => !Input.GetKey(invalidModifier));
            return false;
        }

        public override string ToString()
        {
            return string.Join("+", modifiers.Select((KeyCode m) => m.ToString()).Union(keys.Select((KeyCode k) => k.ToString())).ToArray());
        }
    }
}
