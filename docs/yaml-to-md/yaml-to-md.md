# Admin API documentation
In order to generate the Admin API 2x documentation we need to generate the YAML/JSON file that contains the definition of it. The file is the most important thing if we want to generate the MD files using a library like Docusaurus, widdershins, and so on.

The Admin API 2x uses the library called Swashbuckle to expose the Swagger/OpenAPI definition, in other words, the YAML/JSON file, but this one is generated only when the application is running. Fortunately, the library provides a CLI that we can use to generate the file passing the assembly. Let's see how we can do that:

## Install Swashbuckle.AspNetCore.Cli to generate the JSON/YAML

- Open a command line in the root of the repository and run the following command:
```
dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.5.0
```

Check the .config/dotnet-tools.json file and verify if you have the section called swashbuckle.aspnetcore.cli
```
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "swashbuckle.aspnetcore.cli": {
      "version": "6.5.0",
      "commands": [
        "swagger"
      ],
      "rollForward": false
    }
  }
}
```

- Build the EdFi.Ods.AdminApi project and it should generate the assembly, the location should be in Application\EdFi.Ods.AdminApi\bin\Debug\net10.0\EdFi.Ods.AdminApi.dll

- If everything goes well, we can proceed to generate the api description with following command:
```
dotnet tool run swagger tofile --output ..\..\docs\swagger.yaml --yaml .\bin\Debug\net10.0\EdFi.Ods.AdminApi.dll v2
``` 
For more details check [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#swashbuckleaspnetcorecli)

## Generate documentation
The following libraries to generate the documentation were tested

### widdershins

- Install the tool globaly
```
npm install -g widdershins
```

- Generate the file using the YAML file executing the following 
```
widdershins --search false --language_tabs 'http:HTTP' 'python:PYTHON' 'csharp:CSHARP' --summary swagger.yaml -o adminapi2x.md
```
In this case the MD file has sample code in different programming languages

[Result](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/blob/main/docs/yaml-to-md/adminapi2x.md)

For more details check: [widdershins](https://github.com/Mermade/widdershins)

### Elements - Web Component

- Include the web component in an HTML file

```
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
    <title>Elements in HTML</title>
    <!-- Embed elements Elements via Web Component -->
    <script src="https://unpkg.com/@stoplight/elements/web-components.min.js"></script>
    <link rel="stylesheet" href="https://unpkg.com/@stoplight/elements/styles.min.css">
  </head>
  <body>

    <elements-api
      apiDescriptionUrl="https://raw.githubusercontent.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/ADMINAPI-950/docs/swagger.yaml"
      router="hash"
      layout="sidebar"
    />

  </body>
</html>
```
![Example](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/blob/main/docs/yaml-to-md/elements-example-image.png "Elements Web Component")

For more details check: [Elements](https://github.com/stoplightio/elements?tab=readme-ov-file#web-component)

### Docusaurus - docusaurus-openapi-docs

- Clone the project in the folder. In this case my-website
```
git clone --depth 1 https://github.com/PaloAltoNetworks/docusaurus-template-openapi-docs.git my-website
```
- Mode to the folder
```
cd .\my-website\
```
- Install the dependencies
```
yarn install
```
- Clean the default documents
```
yarn docusaurus clean-api-docs all
```
- Modify the file docusaurus.config.ts, section plugins and set the config
```
plugins: [
    [
      "docusaurus-plugin-openapi-docs",
      {
        id: "openapi",
        docsPluginId: "classic",
        config: {
          adminapi2x: {
            specPath: "examples/swagger.yaml",
            outputDir: "docs/adminapi2x",
            downloadUrl:
              "https://raw.githubusercontent.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/ADMINAPI-950/docs/swagger.yaml",
            sidebarOptions: {
              groupPathsBy: "tag",
              categoryLinkSource: "tag",
            },
          } satisfies OpenApiPlugin.Options,
        } satisfies Plugin.PluginOptions,
      },
    ],
```
- Generate the documentation file using the following command:
```
yarn docusaurus gen-api-docs all
```
- Execute the site and check the results
```
yarn start
```

![Example](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/blob/main/docs/yaml-to-md/docusaurus-example.png "Docusaurus")

For more details check: [Docusaurus](https://github.com/PaloAltoNetworks/docusaurus-openapi-docs)
