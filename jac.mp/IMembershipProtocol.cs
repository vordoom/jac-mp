﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp
{
    public interface IMembershipProtocol
    {
        IEnumerable<Node> Nodes { get; }
        event EventHandler<Node> NodeJoined;
        event EventHandler<Node> NodeFailed;

        void Start();
        void Stop();
    }    
}
