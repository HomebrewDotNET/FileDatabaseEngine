using Sels.FileDatabaseEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.FileDatabaseEngine.Page
{
    public interface IDataPageSourceProvider<T> where T : class
    {
        void Initialize(DirectoryInfo source);

        void Store(T dataObject);
        T Clone(T dataObject);

        void Clear();

        bool IsFree();

        T Load();
    }
}
