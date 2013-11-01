using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastReflectionLib
{
    internal interface IFastReflectionFactory<TKey, TValue>
    {
        TValue Create(TKey key);
    }
}
