﻿using Serilog;
using Serilog.Enrichers.CallerInfo;
using Serilog.Events;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Astra.Hosting
{
    public static class ModuleInitialization
    {
        private static Func<LoggerConfiguration, LoggerConfiguration>? _loggerConfigurationPreprocessor;

        const string CONSOLE_OUTPUT_TEMPLATE =
            "[{Timestamp:HH:mm:ss}] [{Bucket} - {ProcessId}/{ThreadId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        const string FILE_OUTPUT_TEMPLATE =
            "[{Timestamp:MM/dd/yy HH:mm:ss}] [{Bucket} - {ProcessId}/{ThreadId}] [{SourceFile}({Method}:{LineNumber})] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        public static void Initialize([Optional] Func<LoggerConfiguration, LoggerConfiguration>? loggerConfigurationPreprocessor)
        {
            _loggerConfigurationPreprocessor = loggerConfigurationPreprocessor;

            TryRestartWithElevatedPrivileges();
            Log.Logger = InitializeLogger("Main");
        }

        public static ILogger InitializeLogger(string name)
        {
            var loggerConfiguration = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#elif !DEBUG
                .MinimumLevel.Information()
#endif
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Bucket", name, false)
                .WriteTo.Console(outputTemplate: CONSOLE_OUTPUT_TEMPLATE)
                .WriteTo.File("logs/"+ name + "-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: FILE_OUTPUT_TEMPLATE);

            if (_loggerConfigurationPreprocessor != null)
                loggerConfiguration = _loggerConfigurationPreprocessor(loggerConfiguration);
            return loggerConfiguration.CreateLogger();
        }

        public static bool IsElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);

                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetEffectiveUserId() == 0;

            return false;
        }

        public static void TryRestartWithElevatedPrivileges()
        {
            if (IsElevated())
                return;

            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException();

                ProcessStartInfo startInfo = new ProcessStartInfo();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    startInfo.FileName = exePath;
                    startInfo.Verb = "runas";
                    startInfo.UseShellExecute = true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    startInfo.FileName = "sudo";
                    startInfo.Arguments = exePath;
                    startInfo.UseShellExecute = false;
                }
                else throw new PlatformNotSupportedException();

                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to restart with elevated privileges: {ex.Message}");
            }
        }

        [DllImport("libc", EntryPoint = "geteuid")]
        private static extern uint GetEffectiveUserId();
    }
}