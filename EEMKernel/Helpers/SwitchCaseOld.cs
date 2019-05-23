using System;

namespace EemRdx.Helpers
{
    public sealed class SwitchCaseOld<T>
    {
        private readonly T _value;
        private bool _hasMatch = false;
        public SwitchCaseOld(T switchValue)
        {
            _value = switchValue;
        }

        public void Case(T checkValue, Action func)
        {
            if (_value.Equals(checkValue))
            {
                _hasMatch = true;
                func();
            }
        }

        public void Default(Action func)
        {
            if (!_hasMatch) func();
        }
    }
}
