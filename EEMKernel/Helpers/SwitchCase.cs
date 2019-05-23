using System;
using System.Collections.Generic;

namespace EemRdx.Helpers
{
    public sealed class SwitchCase<T>
    {
        private readonly Dictionary<T, Action> Selector;
        private readonly Action Default;

        public SwitchCase(Dictionary<T, Action> Cases, Action DefaultCase)
        {
            this.Selector = Cases;
            this.Default = DefaultCase;
        }

        public void Select(T Variable)
        {
            if (Selector.ContainsKey(Variable)) Selector[Variable]();
            else Default();
        }
    }
}
