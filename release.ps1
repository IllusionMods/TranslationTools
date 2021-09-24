
$topdir = $PSScriptRoot
if ($topdir -eq "") {
    $topdir = "."
}


function Zip-Plugin {
    param([string]$plugin_name)

    $plugin_dir = $topdir + "\bin"

    $dlls = (Get-ChildItem -Path ($plugin_dir) -Filter ($plugin_name + ".dll") -Recurse -Force)
    if (!$dlls) {
        #echo "$plugin_dir : no dlls"
        return
    }
    
    $dll = $dlls[0]
    $version = $dll.VersionInfo.FileVersion.ToString() 
    if (!$version) {
        # echo "$plugin_dir : no version"
        return
    }
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
    Compress-Archive -Path "BepInEx" -Force -CompressionLevel "Optimal" -DestinationPath $zipfile
    popd

    echo $zipfile
    
    Remove-Item -Force -Path $workdir -Recurse
}


$plugin_files = Get-ChildItem -Path ($topdir + "\bin") -Filter "*.dll" -Depth 1 -Recurse -File

foreach ($plugin in $plugin_files) {
    Zip-Plugin $plugin.BaseName
}


