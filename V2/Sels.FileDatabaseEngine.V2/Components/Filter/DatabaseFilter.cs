using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Filter
{
    public abstract class DatabaseFilter<TIdentifier> : IDatabaseFilter<TIdentifier>
    {
        public abstract string Name { get; }

        public abstract bool ActiveForTable(TIdentifier identifier);
    }
}
