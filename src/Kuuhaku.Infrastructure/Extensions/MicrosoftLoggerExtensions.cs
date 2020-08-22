using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class MicrosoftLoggingExtensions
    {
        #region .  Generic  .

        [DebuggerStepThrough]
        public static void Fatal<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogCritical(message, args);

        [DebuggerStepThrough]
        public static void Fatal<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogCritical(exception, message, args);

        [DebuggerStepThrough]
        public static void Error<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogError(message, args);

        [DebuggerStepThrough]
        public static void Error<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogError(exception, message, args);

        [DebuggerStepThrough]
        public static void Warning<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogWarning(message, args);

        [DebuggerStepThrough]
        public static void Warning<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogWarning(exception, message, args);

        [DebuggerStepThrough]
        public static void Info<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogInformation(message, args);

        [DebuggerStepThrough]
        public static void Info<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogInformation(exception, message, args);

        [DebuggerStepThrough]
        public static void Debug<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogDebug(message, args);

        [DebuggerStepThrough]
        public static void Debug<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogDebug(exception, message, args);

        [DebuggerStepThrough]
        public static void Trace<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogTrace(message, args);

        [DebuggerStepThrough]
        public static void Trace<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogTrace(exception, message, args);

        #endregion

        #region .  Non-Generic  .

        [DebuggerStepThrough]
        public static void Fatal(this ILogger logger, String message, params Object[] args)
            => logger.LogCritical(message, args);

        [DebuggerStepThrough]
        public static void Fatal(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogCritical(exception, message, args);

        [DebuggerStepThrough]
        public static void Error(this ILogger logger, String message, params Object[] args)
            => logger.LogError(message, args);

        [DebuggerStepThrough]
        public static void Error(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogError(exception, message, args);

        [DebuggerStepThrough]
        public static void Warning(this ILogger logger, String message, params Object[] args)
            => logger.LogWarning(message, args);

        [DebuggerStepThrough]
        public static void Warning(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogWarning(exception, message, args);

        [DebuggerStepThrough]
        public static void Info(this ILogger logger, String message, params Object[] args)
            => logger.LogInformation(message, args);

        [DebuggerStepThrough]
        public static void Info(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogInformation(exception, message, args);

        [DebuggerStepThrough]
        public static void Debug(this ILogger logger, String message, params Object[] args)
            => logger.LogDebug(message, args);

        [DebuggerStepThrough]
        public static void Debug(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogDebug(exception, message, args);

        [DebuggerStepThrough]
        public static void Trace(this ILogger logger, String message, params Object[] args)
            => logger.LogTrace(message, args);

        [DebuggerStepThrough]
        public static void Trace(this ILogger logger, Exception exception, String message, params Object[] args)
            => logger.LogTrace(exception, message, args);

        #endregion
    }
}
