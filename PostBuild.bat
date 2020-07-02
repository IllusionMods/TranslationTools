@echo off

set POSTBUILD_CONFIG=%~dp0\PostBuild_Config.bat

rem +-------------------------------------------------------------------------------------
rem |
rem | Copies plugins into game on build 
rem | 
rem | Queries registry to find game locations. Assumes they are the standard versions.
rem | 
rem | Locations can be overriden by a file named 'PostBuild_Config.bat' if it 
rem | exists in the solution and contains SET directives for XX_DIR, where XX is the 
rem | target game. Any that are not set via the registry or the override file will be ignored.
rem |
rem | Example contents:
rem | @echo off
rem | set KK_DIR=C:\Games\Koikatu
rem |
rem +-------------------------------------------------------------------------------------


for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\AI-Syoujyo\AI-Syoujyo" /v "INSTALLDIR" 2^>nul') do set "AI_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\AI-Shoujo\AI-Shoujo" /v "INSTALLDIR" 2^>nul') do set "AI_ALT_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\emotioncreators\emotioncreators" /v "INSTALLDIR" 2^>nul') do set "EC_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\HoneySelect\HoneySelect" /v "INSTALLDIR" 2^>nul') do set "HS_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\Software\illusion\HoneySelect2\HoneySelect2" /v "INSTALLDIR" 2^>nul') do set "HS2_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\Koikatu\koikatu" /v "INSTALLDIR" 2^>nul') do set "KK_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\Koikatsu\Koikatsu Party" /v "INSTALLDIR" 2^>nul') do set "KK_PARTY_DIR=%%a"
for /f "tokens=3" %%a in ('REG query "HKEY_CURRENT_USER\SOFTWARE\illusion\PlayHome" /v "INSTALLDIR" 2^>nul') do set "PH_DIR=%%a"

IF EXIST %POSTBUILD_CONFIG% CALL "%POSTBUILD_CONFIG%"

set TARGET_SUBDIR=BepInEx\plugins\TranslationTools

IF "%2" == "KK" (
    IF NOT "%KK_DIR%" == "" (
        set TARGET=%KK_DIR%\%TARGET_SUBDIR%
        call :COPY_TARGET "%1" "%TARGET%" 
        )
    IF NOT "%KK_PARTY_DIR%" == "" (
        set TARGET=%KK_PARTY_DIR%\%TARGET_SUBDIR%
        call :COPY_TARGET "%1" "%TARGET%" 
        )
    goto END
    )

IF "%2" == "KK_ONLY" IF NOT "%KK_DIR%" == "" (
    set TARGET=%KK_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )

IF "%2" == "KK_PARTY" IF NOT "%KK_PARTY_DIR%" == "" (
    set TARGET=%KK_PARTY_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )

IF "%2" == "EC" IF NOT "%EC_DIR%" == "" (
    set TARGET=%EC_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )
    
IF "%2" == "AI" (
    IF NOT "%AI_DIR%" == "" (
        set TARGET=%AI_DIR%\%TARGET_SUBDIR%
        call :COPY_TARGET "%1" "%TARGET%" 
        )
    IF NOT "%AI_ALT_DIR%" == "" (
        set TARGET=%AI_ALT_DIR%\%TARGET_SUBDIR%
        call :COPY_TARGET "%1" "%TARGET%" 
        )
    goto END
)

IF "%2" == "AI_ONLY" IF NOT "%AI_DIR%" == "" (
    set TARGET=%AI_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )

IF "%2" == "AI_INT" IF NOT "%AI_ALT_DIR%" == "" (
    set TARGET=%AI_ALT_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )

    
IF "%2" == "HS" IF NOT "%HS_DIR%" == "" (
    set TARGET=%HS_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )

IF "%2" == "PH" IF NOT "%PH_DIR%" == "" (
    set TARGET=%PH_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%"
    goto END
    )	

IF "%2" == "HS2" IF NOT "%HS2_DIR%" == "" (
    set TARGET=%HS2_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%"
    goto END
    )	
goto NO_TARGET

:COPY_TARGET
IF NOT EXIST "%TARGET%\" mkdir "%TARGET%"

IF EXIST "%TARGET%\" (
    XCOPY /f /y "%1" "%TARGET%"
    exit /b
    )

:TARGET_NOT_EXIST
echo Target dir %TARGET% does not exist and can not create
exit 2

:NO_TARGET
echo Skipping postbuild: '%2' is not a valid target ID or '%2_DIR' is not found in registry or in %POSTBUILD_CONFIG%

:END
