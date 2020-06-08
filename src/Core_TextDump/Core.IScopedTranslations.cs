using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IllusionMods
{
    public interface IScopedTranslations<T> where T:IEnumerable
    {

        IEnumerable<int> Scopes { get; }

        T GetScope(int scope);


    }
}
