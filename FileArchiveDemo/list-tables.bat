@echo off
set DB_PATH=bin\Debug\net8.0\Data\FileArchiveDemo.db

echo Listing tables in %DB_PATH%
echo.

sqlite3 "%DB_PATH%" ".tables"

pause
