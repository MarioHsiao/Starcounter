﻿
using System;
namespace Starcounter.Templates.Interfaces {
    public interface IValueTemplate : IStatefullTemplate {

        object DefaultValueAsObject { get; set; }

        Type InstanceType { get; }

    }
}
