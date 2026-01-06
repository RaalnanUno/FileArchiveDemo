@echo off
set DB_PATH=bin\Debug\net8.0\Data\FileArchiveDemo.db
set OUT_FILE=FileArchiveDemo_schema.sql

echo Writing schema to %OUT_FILE%
echo.

sqlite3 "%DB_PATH%" ".schema" > "%OUT_FILE%"

echo Done.
pause
