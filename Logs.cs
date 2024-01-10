using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.PerfectBooster;

internal static class Logs
{
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateLoggerInstance(PluginInfo.GUID);

    public static void LogDebug(object data)
    {
        Logger.Debug(data.ToString());
    }

    public static void LogError(object data)
    {
        Logger.Error(data.ToString());
    }

    public static void LogInfo(object data)
    {
        Logger.Info(data.ToString());
    }

    public static void LogMessage(object data)
    {
        Logger.Msg(ConsoleColor.White, data.ToString());
    }

    public static void LogWarning(object data)
    {
        Logger.Warning(data.ToString());
    }

    public static void LogNotice(object data)
    {
        Logger.Notice(data.ToString());
    }

    public static void LogSuccess(object data)
    {
        Logger.Success(data.ToString());
    }

    public static void LogException(Exception ex)
    {
        Logger.Exception(ex);
    }
}
