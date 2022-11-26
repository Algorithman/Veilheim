param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath
)

function Create-BepInEx{
    param (
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,

        [Parameter(Mandatory)]
        [ValidateSet('Windows','Unix','Local')]
        [System.String]$DistSystem
    )
    Write-Host "Creating BepInEx in $DistPath"

    # copy needed files for this target system
    Copy-Item -Path "$(Get-Location)\resources\$DistSystem\*" -Destination "$DistPath" -Recurse -Force
    
    # create \BepInEx
    $bepinex = $DistPath.CreateSubdirectory('BepInEx')
    
    # create \BepInEx\core and copy core dlls from build
    $core = $bepinex.CreateSubdirectory('core');
    Copy-Item -Path "$ValheimPath\BepInEx\core\*" -Destination "$core" -Force

    # create \BepInEx\plugins
    $plug = $bepinex.CreateSubdirectory('plugins');

    # create \BepInEx\plugins\$plugin and copy plugin dll from build
    Write-Host "Plugin: $TargetAssembly"
    $modname = $TargetAssembly -Replace('.dll')
    $mod = $plug.CreateSubdirectory("$modname");
    $jotunn = $plug.CreateSubdirectory("Jotunn");
    $mmhook = $plug.CreateSubdirectory('MMHOOK');
    Copy-Item -Path "$TargetPath\*" -Include $TargetAssembly -Destination "$mod" -Force
    Copy-Item -Path "$TargetPath\*" -Include "Jotunn.dll" -Destination "$jotunn" -Force
    Copy-Item -Path "$ValheimPath\BepInEx\plugins\MMHOOK\*" -Destination "$mmhook" -Force

    # return basepath as DirectoryInfo
    return $base
}

function Copy-Corlib{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,
        
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$LibPath
    )
    Write-Host "Copy unstripped_corlib to $DistPath"

    $rel = $DistPath.CreateSubdirectory('unstripped_corlib')
    Copy-Item -Path "$LibPath\*" -Filter '*.dll' -Destination "$rel" -Force
}

function Make-Archive{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath
    )

    $rel = $DistPath.Parent.FullName
    $zip = $DistPath.Name + ".zip"
    
    Write-Host "Creating archive $zip for $DistPath"

    Compress-Archive -Path "$DistPath\*" -DestinationPath "$rel\$zip" -Force
}

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$ValheimPath\unstripped_corlib",
 "$(Get-Location)\resources",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

# Plugin name without ".dll"
$modname = "$TargetAssembly" -Replace('.dll')

if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
    
    # create \BepInEx\plugins\$plugin and copy plugin dll from build
    $plug = New-Item -Type Directory -Path "$ValheimPath\BepInEx\plugins\$modname" -Force
    Write-Host "Plugin: $TargetAssembly"
    Copy-Item -Path "$TargetPath\$modname.dll" -Destination "$plug" -Force
    
    # Create the mdb file
    $pdb = "$TargetPath\$modname.pdb"
    if (Test-Path -Path "$pdb") {
        Write-Host "Create and copy mdb file"
        Invoke-Expression "& `"$(Get-Location)\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
        Copy-Item -Path "$TargetPath\$modname.dll.mdb" -Destination "$plug" -Force
    }

  
    # set dnspy debugger env
    #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=y,no-hide-debugger'
    #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2','','User')

}

if ($Target.Equals("Release")) {
    $rel = New-Item -ItemType Directory -Path "$(Get-Location)\release" -Force
    $lib = Get-Item -Path "$ValheimPath\unstripped_corlib"

    Write-Host "Building release packages to $rel"
    
    # create all distros as folders and zip
    ('Windows','Unix') | % {
        $dist = New-Item -ItemType Directory -Path "$rel\$_" -Force;
        Create-BepInEx -DistPath $dist -DistSystem $_
        Copy-Corlib -DistPath $dist -LibPath $lib
        Make-Archive -DistPath $dist
        $dist.Delete($true);
    }
}

# Pop Location
Pop-Location