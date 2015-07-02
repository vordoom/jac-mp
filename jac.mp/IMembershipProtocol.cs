using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp
{
    public interface IMembershipProtocol<T>
    {
        IEnumerable<Node<T>> Nodes { get; }
        event EventHandler<Node<T>> NodeJoined;
        event EventHandler<Node<T>> NodeFailed;

        void Start();
        void Stop();
    }    
}
