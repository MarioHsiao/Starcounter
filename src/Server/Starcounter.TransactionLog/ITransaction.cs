﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public interface ITransaction
    {
        byte[] payload { get; }

        //to do expose fields for filtering
    }
}