if(Test-Path Variable:\OctopusParameters)
{
    [Environment]::SetEnvironmentVariable("EdFi.ApiLoader.Console.$($OctopusParameters["PromotionEnvironment"])",$OctopusParameters["OctopusOriginalPackageDirectoryPath"] , "Machine")
}
