#if false
using System;
using System.Collections.Generic;
using System.Text;
using Sc.Server.Network;
using System.Diagnostics;
using Starcounter.Poleposition.Util;
using Starcounter.Poleposition.Framework;
using Starcounter.Poleposition.Internal;
using System.Web;

namespace Starcounter.Poleposition.Entrances
{
public class PolePositionServer : IIoHandler
{
	private static readonly Sc.Server.Internal.LogSource log = new Sc.Server.Internal.LogSource(
        System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

    #region IIoHandler Members

    public void ChannelClosed(Channel channel)
    {
    }

    public void ChannelOpened(Channel channel, bool serverInitiative)
    {
        channel.Read();
    }

    public void ExceptionCaught(Channel channel, Exception exception)
    {
        log.LogWarning("Exception in PolePosition channel {0}: {1}", channel.UniqueID, exception);
    }

    public void ChannelIdle(Channel channel, IdleStatus status)
    {
    }

    public void MessageRead(Channel channel, ByteBuffer message)
    {
        try
        {
            byte[] inbytes = new byte[message.Length];
            message.CopyToBytes(0, inbytes, 0, message.Length);
            var qs = new QueryString(Encoding.UTF8.GetString(inbytes));
            var checksum = RunLapFromQueryString(qs);
            SendSingleKeyResponse(channel, ParamNames.Checksum, checksum.ToString(), false);
        }
        catch (Exception e)
        {
            SendSingleKeyResponse(channel, "Error", e.ToString(), true);
        }
        channel.Read();
    }

    private long RunLapFromQueryString(QueryString parameters)
    {
        var setup = ReadSetup(parameters);
        var driver = RegistryHolder.Drivers.Instantiate(parameters[ParamNames.Driver], setup);
        driver.RunLap(parameters[ParamNames.Lap]);
        return setup.CheckSum;
    }

    private Setup ReadSetup(QueryString parameters)
    {
        string value;
        Setup setup = new Setup();
        if (parameters.TryGetValue(ParamNames.Checksum, out value))
        {
            setup.CheckSum = long.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.CommitInterval, out value))
        {
            setup.CommitInterval = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.ObjectCount, out value))
        {
            setup.ObjectCount = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.SelectCount, out value))
        {
            setup.SelectCount = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.UpdateCount, out value))
        {
            setup.UpdateCount = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.TreeWidth, out value))
        {
            setup.TreeWidth = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.TreeDepth, out value))
        {
            setup.TreeDepth = int.Parse(value);
        }
        if (parameters.TryGetValue(ParamNames.ObjectCount, out value))
        {
            setup.ObjectCount = int.Parse(value);
        }
        return setup;
    }

    private static void SendSingleKeyResponse(Channel channel, string key, string value, bool urlEncode)
    {
        using(ByteBuffer response = new ByteBuffer())
        {
            if (urlEncode)
            {
                value = HttpUtility.UrlDecode(value, Encoding.UTF8);
            }
            byte[] outbytes = Encoding.UTF8.GetBytes(key + "=" + value);
            response.CopyFromBytes(0, outbytes, 0, outbytes.Length);
            channel.Write(response);
        }
    }

    public void Setup(Application hostContext)
    {
        RegistryHolder.Init();
    }

    #endregion

    /// <summary>
    /// Lazy init of the driver registry.
    /// </summary>
    private static class RegistryHolder
    {
        public static void Init()
        {
            // Does nothing but makes sure class is loaded
        }

        public static readonly DriverRegistry Drivers =
            DriverRegistry.OfAssembly(typeof(PolePositionServer).Assembly);

        static RegistryHolder()
        {
#if DEBUG
            log.LogWarning("This version of PolePosition has been built as DEBUG - build as RELEASE for best performance");
#endif
        }
    }

}

public static class ParamNames
{
    public const string Driver = "Driver";
    public const string Lap = "Lap";
    public const string Checksum = "Checksum";
    public const string CommitInterval = "CommitInterval";
    public const string ObjectCount = "ObjectCount";
    public const string SelectCount = "SelectCount";
    public const string UpdateCount = "UpdateCount";
    public const string TreeWidth = "TreeWidth";
    public const string TreeDepth = "TreeDepth";
    public const string ObjectSize = "ObjectSize";
}

}
#endif