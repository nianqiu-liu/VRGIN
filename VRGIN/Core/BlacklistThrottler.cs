using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRGIN.Core
{
    internal class BlacklistThrottler : ProtectedBehaviour
    {
        public HashSet<Type> Targets = new HashSet<Type>();

        protected override void OnStart()
        {
            Targets.Add(typeof(Camera));
            base.OnStart();
        }

        protected override void OnUpdate()
        {
            foreach (var item in from c in GetComponents<Behaviour>()
                                 where Targets.Contains(c.GetType())
                                 select c)
                item.enabled = false;
            base.OnUpdate();
        }
    }
}
