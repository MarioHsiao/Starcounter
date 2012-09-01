// Copyright (c) 2004 SICS AB. All rights reserved.
//
#undef APPSESSION
namespace se.sics.prologbeans
{
using System;
//UPGRADE_TODO: Interface javax.servlet.http.HttpSessionBindingListener was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
// [PD] FIXME: Until servlet sessions are implemented we don't need this class
#if APPSESSION
/// <summary> <c>AppSession</c>
/// </summary>
public class AppSession : HttpSessionBindingListener
{
    virtual public PrologSession PrologSession
    {
        // AppSession constructor
        get
        {
            return session;
        }
    }

    private PrologSession session;

    public AppSession(PrologSession session)
    {
        this.session = session;
    }

    //UPGRADE_TODO: Class javax.servlet.http.HttpSessionBindingEvent was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
    public virtual void  valueBound(HttpSessionBindingEvent event_Renamed)
    {
        //     System.out.println("Value bound:" + event.getName() + " = " +
        //           event.getValue());
    }

    //UPGRADE_TODO: Class javax.servlet.http.HttpSessionBindingEvent was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
    public virtual void  valueUnbound(HttpSessionBindingEvent event_Renamed)
    {
        //     System.out.println("Value unbound:" + event.getName() + " = " +
        //           event.getValue());
        //UPGRADE_TODO: Method javax.servlet.http.HttpSessionBindingEvent.getName was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
        if ("prologbeans.session".Equals(event_Renamed.getName()))
        {
            session.endSession();
        }
    }
}
// AppSession
#endif
}
