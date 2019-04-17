// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

package main

import (
	"flag"
	"fmt"
	"log"
	"path/filepath"
	"startupscriptgenerator/common"
)

func main() {
	common.PrintVersionInfo()

	appPathPtr := flag.String(
		"appPath",
		".",
		"The path to the published output of the application that is going to be run, e.g. '/home/site/wwwroot/'. " +
		"Default is current directory.")
	runFromPathPtr := flag.String(
		"runFromPath",
		"",
		"The path to the directory where the output is copied and run from there.")
	sourcePathPtr := flag.String(
		"sourcePath",
		"",
		"[Optional] The path to the application that is being deployed, " +
		"Ex: '/home/site/repository/src/ShoppingWebApp/'.")
	bindPortPtr := flag.String("bindPort", "", "[Optional] Port where the application will bind to. Default is 8080")
	userStartupCommandPtr := flag.String(
		"userStartupCommand",
		"",
		"[Optional] Command that will be executed to start the application up.")
	outputPathPtr := flag.String("output", "run.sh", "Path to the script to be generated.")
	defaultAppFilePathPtr := flag.String(
		"defaultAppFilePath",
		"",
		"[Optional] Path to a default dll that will be executed if the entrypoint is not found. " +
		"Ex: '/opt/startup/aspnetcoredefaultapp.dll'")
	flag.Parse()

	fullAppPath := ""
	if *appPathPtr != "" {
		fullAppPath = common.GetValidatedFullPath(*appPathPtr)
	}

	common.SetGlobalOperationId(fullAppPath)

	fullRunFromPath := ""
	if *runFromPathPtr != "" {
		fullRunFromPath, _ = filepath.Abs(*runFromPathPtr)
	}

	fullSourcePath := ""
	if *sourcePathPtr != "" {
		fullSourcePath = common.GetValidatedFullPath(*sourcePathPtr)
	}

	fullDefaultAppFilePath := ""
	if *defaultAppFilePathPtr != "" {
		fullDefaultAppFilePath = common.GetValidatedFullPath(*defaultAppFilePathPtr)
	}

	if fullRunFromPath != "" {
		fmt.Println("Intermediate directory option was specified, so copying content...")
		common.CopyToDir(fullAppPath, fullRunFromPath)
		fullAppPath = fullRunFromPath
	}

	buildManifest := common.GetBuildManifest(fullAppPath)
	if buildManifest.ZipAllOutput == "true" {
		fmt.Println("Read build manifest file and found output has been zipped. Extracting it...")
		common.ExtractZippedOutput(fullAppPath)
	}

	entrypointGenerator := DotnetCoreStartupScriptGenerator{
		SourcePath:          fullSourcePath,
		AppPath:             fullAppPath,
		BindPort:            *bindPortPtr,
		UserStartupCommand:  *userStartupCommandPtr,
		DefaultAppFilePath:  fullDefaultAppFilePath,
	}

	command := entrypointGenerator.GenerateEntrypointScript()
	if command == "" {
		log.Fatal("Could not generate a startup script.")
	}

	common.WriteScript(*outputPathPtr, command)
}
