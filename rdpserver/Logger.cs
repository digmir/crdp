using log4net;
using System;
using System.Reflection;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "logconf.xml", Watch = true)]

public class Logger
{
    private static ILog logger = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);

    public static void Trace(Exception e)
    {
        string msg = e.Message + e.StackTrace;

        if (logger == null)
        {
            logger = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
        }

        System.Diagnostics.StackFrame sf = null;
        System.Reflection.MethodBase caller = null;
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
        int i;
        for (i = 0; i < st.FrameCount; i++)
        {
            sf = st.GetFrame(i);
            System.Reflection.MethodBase tcaller = sf.GetMethod();
            if (string.IsNullOrEmpty(tcaller.DeclaringType.FullName))
            {
                caller = tcaller;
                break;
            }
            if (string.Compare(tcaller.DeclaringType.FullName, MethodInfo.GetCurrentMethod().DeclaringType.FullName, true) == 0)
            {
                continue;
            }
            if (string.Compare(tcaller.Name, MethodInfo.GetCurrentMethod().Name, true) == 0)
            {
                continue;
            }
            caller = tcaller;
            break;
        }
        if (i >= st.FrameCount)
        {
            logger.Info(msg);
            return;
        }

        System.Diagnostics.StackFrame locationFrame = st.GetFrame(i);
        string szFileName = locationFrame.GetFileName();
        try
        {
            szFileName = szFileName.Substring(szFileName.LastIndexOf('\\') + 1);
        }
        catch
        {
            szFileName = "";
        }

        string szCaller = szFileName + "[" + string.Format("{0,3:D}", locationFrame.GetFileLineNumber()) + "] ";
        if (caller != null)
        {
            if (!string.IsNullOrEmpty(caller.DeclaringType.FullName))
            {
                szCaller += caller.DeclaringType.FullName + ".";
            }
            if (!string.IsNullOrEmpty(caller.Name))
            {
                szCaller += caller.Name;
            }
            else
            {
                szCaller += "?";
            }
        }
        logger.Info(szCaller + ": " + msg);
    }

    public static void Trace(string msg)
    {
        if (logger == null)
        {
            logger = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
        }

        System.Diagnostics.StackFrame sf = null;
        System.Reflection.MethodBase caller = null;
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
        int i;
        for (i = 0; i < st.FrameCount; i++)
        {
            sf = st.GetFrame(i);
            System.Reflection.MethodBase tcaller = sf.GetMethod();
            if (string.IsNullOrEmpty(tcaller.DeclaringType.FullName))
            {
                caller = tcaller;
                break;
            }
            if (string.Compare(tcaller.DeclaringType.FullName, MethodInfo.GetCurrentMethod().DeclaringType.FullName, true) == 0)
            {
                continue;
            }
            if (string.Compare(tcaller.Name, MethodInfo.GetCurrentMethod().Name, true) == 0)
            {
                continue;
            }
            caller = tcaller;
            break;
        }
        if (i >= st.FrameCount)
        {
            logger.Info(msg);
            return;
        }

        System.Diagnostics.StackFrame locationFrame = st.GetFrame(i);
        string szFileName = locationFrame.GetFileName();
        try
        {
            szFileName = szFileName.Substring(szFileName.LastIndexOf('\\') + 1);
        }
        catch
        {
            szFileName = "";
        }

        string szCaller = szFileName + "[" + string.Format("{0,3:D}", locationFrame.GetFileLineNumber()) + "] ";
        if (caller != null)
        {
            if (!string.IsNullOrEmpty(caller.DeclaringType.FullName))
            {
                szCaller += caller.DeclaringType.FullName + ".";
            }
            if (!string.IsNullOrEmpty(caller.Name))
            {
                szCaller += caller.Name;
            }
            else
            {
                szCaller += "?";
            }
        }
        logger.Info(szCaller + ": " + msg);
    }
}
