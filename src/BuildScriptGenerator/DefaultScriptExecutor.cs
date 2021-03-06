﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Oryx.Common;

namespace Microsoft.Oryx.BuildScriptGenerator
{
    internal class DefaultScriptExecutor : IScriptExecutor
    {
        private readonly ILogger<DefaultScriptExecutor> _logger;

        public DefaultScriptExecutor(ILogger<DefaultScriptExecutor> logger)
        {
            _logger = logger;
        }

        public int ExecuteScript(
            string scriptPath,
            string[] args,
            string workingDirectory,
            DataReceivedEventHandler stdOutHandler,
            DataReceivedEventHandler stdErrHandler)
        {
            int exitCode = ProcessHelper.TrySetExecutableMode(scriptPath, workingDirectory);
            if (exitCode != ProcessConstants.ExitSuccess)
            {
                _logger.LogError(
                    "Failed to set execute permission on script {scriptPath} ({exitCode})",
                    scriptPath,
                    exitCode);
                return exitCode;
            }

            exitCode = ExecuteScriptInternal(scriptPath, args, workingDirectory, stdOutHandler, stdErrHandler);
            if (exitCode != ProcessConstants.ExitSuccess)
            {
                try
                {
                    var directoryStructureData = OryxDirectoryStructureHelper.GetDirectoryStructure(workingDirectory);
                    _logger.LogTrace(
                        "logDirectoryStructure",
                        new Dictionary<string, string> { { "directoryStructure", directoryStructureData } });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception caught");
                }
                finally
                {
                    _logger.LogError("Execution of script {scriptPath} failed ({exitCode})", scriptPath, exitCode);
                }
            }

            return exitCode;
        }

        protected virtual int ExecuteScriptInternal(
            string scriptPath,
            string[] args,
            string workingDirectory,
            DataReceivedEventHandler stdOutHandler,
            DataReceivedEventHandler stdErrHandler)
        {
            int exitCode;
            using (var timedEvent = _logger.LogTimedEvent(
                "ExecuteScript",
                new Dictionary<string, string> { { "scriptPath", scriptPath } }))
            {
                exitCode = ProcessHelper.RunProcess(
                    scriptPath,
                    args,
                    workingDirectory,
                    standardOutputHandler: stdOutHandler,
                    standardErrorHandler: stdErrHandler,
                    waitTimeForExit: null); // Do not provide wait time as the caller can do this themselves.
                timedEvent.AddProperty("exitCode", exitCode.ToString());
            }

            return exitCode;
        }
    }
}