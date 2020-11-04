
$topdir = $PSScriptRoot
if ($topdir -eq "") {
    $topdir = "."
}


function Zip-Plugin {
    param([string]$dll_name)



    $plugin_dir = $topdir + "\bin"
    
    $dll = (Get-ChildItem -Path ($plugin_dir) -Filter $dll_name -Recurse -Force)[0]
    $version = $dll.VersionInfo.FileVersion.ToString() 

    $srcpath = "src\" + $dll.BaseName
    
    if (-Not (Test-Path $srcpath)) {
        return
    }

    if ($version -eq "0.0.0.0") {
        return
    }


   
    $workdir = $topdir + "\work"
    $destdir = $workdir + "\BepInEx\plugins\TranslationTools"

    $zipfile = $topdir + "\dist\" + $dll.BaseName + ".v" + $version + ".zip"
    
    if (Test-Path $zipfile) {
        return
    }

    if (Test-Path $workdir) {
        Remove-Item -Force -Path $workdir -Recurse
    }

    $dummy = New-Item -ItemType Directory -Force -Path $destdir 

    Copy-Item -Path $dll.FullName -Destination $destdir -Recurse -Force
    
    $dummy = New-Item -ItemType Directory -Force -Path ($topdir + "\dist")

    pushd $workdir
    #Compress-Archive -Path "BepInEx" -Force -CompressionLevel "Optimal" -DestinationPath $zipfile
    & 7z.exe a -mx9 -tzip $zipfile BepInEx
    popd

    echo $zipfile
    
    Remove-Item -Force -Path $workdir -Recurse
}


$plugin_files = Get-ChildItem -Path ($topdir + "\bin") -Filter "*.dll" -Depth 1 -Recurse -File

foreach ($plugin in $plugin_files) {
    Zip-Plugin $plugin
}


