using System;
using System.Collections.Generic;
using System.Text;
using LINQPad.Extensibility.DataContext;

namespace Aerospike.Database.LINQPadDriver
{
    public interface ILPExplorer
    {
        public ExplorerItem CreateExplorerItem();
    }
}
