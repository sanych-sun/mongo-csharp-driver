#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

python ./evergreen/release-notes.py 2.22.0
