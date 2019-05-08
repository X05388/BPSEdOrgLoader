This is a console application created to Read the name of education organization from input file from a shared drive and upload them to the ODS if they are not already available. This application Also make an API call to get the list of all the schools and checks if the DeptId and DeptName from the input file is already present in the ODS database in the "edfi.EducationOrganization" table , and if it does not exist there then it creates an XML including the education organization Id to be inserted and make a call to the API loader with the XML as input.

You can also run this application by passing command line parameters as follows or you can also set them in app-settings in configuration file:

1. CrossWalkSchoolApiUrl : API URL to fetch the list of Schools 
2. CrossWalkOAuthUrl : API URL to get the token using (CrossWalkKey,CrossWalkSecret) 
3. CrossWalkKey : Key for getting token using API URL (CrossWalkOAuthUrl)
4. CrossWalkSecret : Secret for getting token using API URL (CrossWalkOAuthUrl)
5. DataFilePath : Input text file path
6. CrossWalkFilePath : This can be used for reading existing dept or schools instead of calling an API , this is not in use now so you can ignore
7. XMLOutputPath : Path to save the XML which will be used as an input to API loader
8. OAuthKey: API key used by API loader to upload the data using API
9. OAuthSecret : API secret used by API loader to upload the data using API
10. OAuthUrl: URL used by API loader for authentication
11. ApiUrl : API URL used by API loader to upload the education organization
12.SwaggerUrl : URL used by API loader for loading matadata
13.  SchoolYear: School year used by API loader 
14. BakupDays: Number of days used by API loader for keeping backup files , default is 15 days
15. XsdFolder : XSD file location used by API loader
16. WorkingFolder : Folder used by API loader to keep the metadata and hash files so that API loader does not try to upload the same file again and again
17. ApiLoaderExePath : Path to the API loader exe 

You can also configure the Logging in case you want to troubleshoot any issue in production using following settings:
<level value="Info" /> 
Here Value could be Error, Info, Debug, Trace
