using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Filter
{
    public interface IDatabaseFilter<TIdentifier>
    {
        string Name { get; }

        bool ActiveForTable(TIdentifier identifier);
    }
}
