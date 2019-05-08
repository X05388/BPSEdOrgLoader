This is a console application created by EdFi to upload data using XML by calling API so instead of directly inserting recording into the ODS database like bulk load utility, it calls API to load data.

If you have XML created already then you can also run this application directly by passing command line parameters as follows or you can also set them in app-settings in configuration file:

1. ApiUrl : API URL used by API loader to upload the education organization
2. SwaggerUrl : URL used by API loader for loading matadata
3. SchoolYear: School year used by API loader 
4. XsdFolder : XSD file location used by API loader
5. InterchangeOrderFolder : Metadata folder path
6. WorkingFolder : Folder used by API loader to keep the metadata and hash files so that API loader does not try to upload the same file again and again
7. DataFolder: Path to save the XML which will be used as an input to API loader 
8. OAuthKey: API key used by API loader to upload the data using API
9. OAuthSecret : API secret used by API loader to upload the data using API
10.  OAuthUrl: URL used by API loader for authentication

You can also configure the Logging in case you want to troubleshoot any issue in production using following settings:
<threshold value="Info" />
Here Value could be Error, Info, Debug, Trace
