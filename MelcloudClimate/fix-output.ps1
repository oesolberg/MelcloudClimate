Param(
  [string]$configuration,
  [string]$keep
)

if ($configuration -eq "") {
    $configuration = "Debug"
}

if ($keep -eq "") {
    [xml]$proj = get-content (gci $PSScriptRoot\* -Include *.csproj, *.vbproj | select -First 1 -ExpandProperty FullName) 
    $keep = $($proj.Project.PropertyGroup.AssemblyName[0])
}

$keep = $keep.Replace("HSPI_", "")

"Keeping $keep"

$source = "$PSScriptRoot\bin\$configuration"
$destination = "$source\bin\$keep"

if ((test-path $destination) -eq $False) {
    mkdir $destination -Verbose
}


gci $source -File | where { $_.Name -notmatch ".*$keep.*" } | % {
    Move-Item $_.FullName -Destination $destination -Force -Verbose
}

""
""
"*** Checking for subfolders with files that need to be added to output ***"
""
### Test for the following paths:
### HTML\Help\
### Html\images\
### Html\includes\
### scripts\
### scripts\includes\
### data\

#1. Check if the folder exists
#2. Check if the folder contains files
#3. Copy files to destination folder
$folderCollection=@("html\help\","html\images\","html\includes\","scripts\includes\","data\");
ForEach($folder in $folderCollection)
{
    #Sett correct foldername
    $sourceFolder=[io.path]::combine($PSScriptRoot,$folder);
    #Check if we have any files in the folder
    
    if ((test-path $sourceFolder) -eq $True) 
    {        
        $numberOfFoundFiles=( Get-ChildItem $sourceFolder | Measure-Object ).Count;
        if($numberOfFoundFiles -gt 0)
        {
            #We found some files, now try to move them to the correct location
			"Copying files from $sourceFolder"
            
            $destinationFolder=[io.path]::combine($source,$folder,$keep)
            if ((test-path $destinationFolder) -eq $False) 
            {
                mkdir $destinationFolder -Verbose
            }
            #Move the files
            Get-ChildItem -Path $sourceFolder   | ForEach-Object -Process {
                Copy-Item $_.FullName -Destination $destinationFolder -Recurse -Force -Verbose
            }
        }
    }
	else{
	"Could not find folder $sourceFolder"
	}
}