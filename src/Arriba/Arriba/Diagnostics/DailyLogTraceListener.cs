﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Arriba.Diagnostics.Tracing;

namespace Arriba.Diagnostics
{
    /// <summary>
    ///  DailyLogTraceListener is a simple trace listener which logs by appending
    ///  to a sharable file. It opens the file each time, so it should not be used
    ///  for large log volumes.
    ///  
    ///  By default, the log file is Logs\[ExeName]\yyyy-MM-dd.log.
    /// </summary>
    public class DailyLogTraceListener : TraceListener
    {
        private string LogFilePath { get; set; }
        private ILoggingContext Log { get; }

        public DailyLogTraceListener(ILoggingContextFactory log) 
            : this(GetDefaultLogFilePath())
        {
            Log = log.Initialize<DailyLogTraceListener>();
        }

        public DailyLogTraceListener(string logFilePath)
        {
            this.LogFilePath = logFilePath;

            string logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
        }

        private static string GetDefaultLogFilePath()
        {
            string executableName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            return String.Format(@"Logs\{0}\{1:yyyy-MM-dd}.log", executableName, DateTime.Today);
        }

        public override void Write(string message)
        {
            try
            {
                File.AppendAllText(this.LogFilePath, message);
            }
            catch (IOException ex)
            {
                Log.TrackExceptionOnSave(ex, null);
            }
        }

        public override void WriteLine(string message)
        {
            try
            {
                File.AppendAllText(this.LogFilePath, message + Environment.NewLine);
            }
            catch (IOException ex)
            {
                Log.TrackExceptionOnSave(ex, null);
            }
        }
    }
}
