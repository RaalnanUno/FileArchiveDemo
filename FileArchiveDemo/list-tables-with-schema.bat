@echo off
set DB_PATH=bin\Debug\net8.0\Data\FileArchiveDemo.db

echo Dumping table schemas from %DB_PATH%
echo.

sqlite3 "%DB_PATH%" ^
".headers on" ^
".mode column" ^
"SELECT name AS TableName, sql AS CreateStatement
 FROM sqlite_master
 WHERE type='table'
 ORDER BY name;"

pause
