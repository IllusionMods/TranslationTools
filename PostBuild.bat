REM @echo off

set POSTBUILD_CONFIG=%~dp0\PostBuild_Config.bat

rem +-------------------------------------------------------------------------------------
rem |
rem | Copies plugins into game on build if a file named 'PostBuild_Config.bat'
rem | exists in the solution and contains SET directives for XX_DIR, where XX is the 
rem | target game. Any that are not set will be ignored.
rem |
rem | Example contents:
rem | @echo off
rem | set KK_DIR=C:\Games\Koikatu
rem |
rem +-------------------------------------------------------------------------------------

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

IF "%2" == "EC" IF NOT "%EC_DIR%" == "" (
    set TARGET=%EC_DIR%\%TARGET_SUBDIR%
    call :COPY_TARGET "%1" "%TARGET%" 
    goto END
    )
    
IF "%2" == "AI" IF NOT "%AI_DIR%" == "" (
    set TARGET=%AI_DIR%\%TARGET_SUBDIR%
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

goto NO_TARGET

:COPY_TARGET
IF NOT EXIST "%TARGET%\" mkdir "%TARGET%"

IF EXIST "%TARGET%\" (
    XCOPY /f /y "%1" "%TARGET%"
    exit /b
    )

:TARGET_NOT_EXIST
echo Target dir %TARGET% does not exist and can not create

:NO_TARGET
echo Skipping postbuild: %2 is not a valid target ID or %2_DIR is not set in %POSTBUILD_CONFIG%

:END