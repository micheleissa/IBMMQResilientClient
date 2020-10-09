using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBMMQResilientClient
{
    public interface IQueueReader
    {
        QueueMessage Dequque();
        QueueMessage Peek();
    }
}
