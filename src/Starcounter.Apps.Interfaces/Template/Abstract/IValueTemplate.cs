
using System;
namespace Starcounter.Templates.Interfaces {
    public interface IValueTemplate : IStatefullTemplate {

        object DefaultValueAsObject { get; set; }

        new Type InstanceType { get; }

    }
}
