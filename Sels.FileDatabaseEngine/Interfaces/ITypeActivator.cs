using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Interfaces
{
    public interface ITypeActivator<T>
    {
        T CreateInstanceFromType(Type type);
    }
}
