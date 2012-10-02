

namespace Starcounter.Templates.Interfaces {


    public interface IAppFactory {

        IApp CreateApp();
        IAppTemplate CreateAppTemplate();
        IStringTemplate CreateStringTemplate();
        IDoubleTemplate CreateDoubleTemplate();
        IBoolTemplate CreateBoolTemplate();

    }

}