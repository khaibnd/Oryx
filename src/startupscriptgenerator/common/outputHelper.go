// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

package common

import (
	"bytes"
	"fmt"
	"os/exec"
	"strings"
)

func ExtractZippedOutput(appFolder string) {
	zipFileName := "oryx_output.tar.gz"
	scriptPath := "/tmp/extractZippedOutput.sh"
	scriptBuilder := strings.Builder{}
	scriptBuilder.WriteString("#!/bin/sh\n")
	scriptBuilder.WriteString("set -e\n\n")
	scriptBuilder.WriteString("cd \"" + appFolder + "\"\n")
	scriptBuilder.WriteString("if [ -f \"" + zipFileName + "\" ]; then\n")
	scriptBuilder.WriteString(
		"    echo \"Found '" + zipFileName + "' under '" + appFolder + "'. Extracting it's contents into it...\"\n")
	scriptBuilder.WriteString("    tar -xzf " + zipFileName + "\n")
	scriptBuilder.WriteString("    echo Done.\n")
	scriptBuilder.WriteString("    echo Deleting the file '" + zipFileName + "'...\n")
	scriptBuilder.WriteString("    rm -f \"" + zipFileName + "\"\n")
	scriptBuilder.WriteString("    echo \"Done.\"\n")
	scriptBuilder.WriteString("fi\n\n")

	WriteScript(scriptPath, scriptBuilder.String())
	ExecuteCommand("/bin/sh", "-c", scriptPath)
}

func CopyToDir(srcDir string, destDir string) {
	scriptPath := "/tmp/copyToIntermediateDir.sh"
	scriptBuilder := strings.Builder{}
	scriptBuilder.WriteString("#!/bin/sh\n")
	scriptBuilder.WriteString("set -e\n\n")
	scriptBuilder.WriteString("if [ -d \"" + destDir + "\" ]; then\n")
	scriptBuilder.WriteString("    echo Directory '" + destDir + "' already exists. Deleting it...\n")
	scriptBuilder.WriteString("    rm -rf \"" + destDir + "\"\n")
	scriptBuilder.WriteString("    echo \"Done.\"\n")
	scriptBuilder.WriteString("fi\n")
	scriptBuilder.WriteString("mkdir -p \"" + destDir + "\"\n")
	scriptBuilder.WriteString("echo Copying content from '" + srcDir + "' to directory '" + destDir + "'...\n")
	scriptBuilder.WriteString("cp -rf \"" + srcDir + "\"/* \"" + destDir + "\"\n")
	scriptBuilder.WriteString("echo \"Done.\"\n")

	WriteScript(scriptPath, scriptBuilder.String())
	ExecuteCommand("/bin/sh", "-c", scriptPath)
}

func ExecuteCommand(name string, arg ...string) {
	command := exec.Command(name, arg...)
	var stdOut bytes.Buffer
	var stdErr bytes.Buffer
	command.Stdout = &stdOut
	command.Stderr = &stdErr
	err := command.Run()
	if err != nil {
		fmt.Println(fmt.Sprint(err) + ": " + stdErr.String())
		panic(err)
	}
	fmt.Println(stdOut.String())
}