﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp
{
    public class Node
    {
        public Uri Address { get; private set; }

        public Node(Uri address)
        {
            this.Address = address;
        }
    }
}
