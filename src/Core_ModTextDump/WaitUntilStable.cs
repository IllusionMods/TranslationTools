using System.Collections;
using System.Linq;
using UnityEngine;

namespace IllusionMods
{
    internal class WaitUntilStable : CustomYieldInstruction

    {
        private readonly int _framesUntilStable;
        private readonly ICollection _target;
        private int _lastCount = -1;
        private int _stableCount;

        internal WaitUntilStable(ICollection target, int framesUntilStable = 3)
        {
            _framesUntilStable = framesUntilStable;
            _target = target;
        }

        public override bool keepWaiting => !IsDone();

        private bool IsDone()
        {
            var lastCount = _lastCount;
            _lastCount = _target.Count;
            if (_lastCount != lastCount)
            {
                _stableCount = 0;
                return false;
            }

            _stableCount++;
            return _stableCount > _framesUntilStable;
        }
    }
}
