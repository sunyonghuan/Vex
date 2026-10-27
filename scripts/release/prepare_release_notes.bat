@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0prepare_release_notes.ps1" -RepositoryRoot "%~dp0..\.." %*
