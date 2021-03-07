# Azure Functions Unit/Integration Testing Sample

This repository contains a sample application to showcase how one can implement Unit and Integration testing for an Azure Functions application.  
Its essentially a companion for my series of blog posts on **Test Driven Development of Serverless Apps with Azure Functions**, which can be accessed [at this link](https://sntnupl.com/azure-functions-testing-guide-p1).  

## Getting Started  

1. Clone this repo: `git clone https://github.com/sntnupl/azure-functions-sample-unittest-integrationtest`
1. Open `SampleApp.sln` in Visual Studio.
1. Build the solution
1. Create a file titled `local.settings.json` in `InvoiceProcessor` project. 
   - A sample file titled `sample.local.settings.json` is provided, you can use this file to create the above. Remember to replace the `<PLACEHOLDER>` with actual values though.
1. Create a file titled `appsettings.json` in `InvoiceProcessor.Tests.Integration` project.
   - Again, a sample file titled `sample.appsettings.json` is provided, you can use this file to create the above and remember to replace the `<PLACEHOLDER>` as before.
1. Run all tests in the solution (using the Visual Studio test explorer).


**Pre-requisites**
+ Follow official document by Microsoft on [Getting Started with Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-get-started?pivots=programming-language-csharp) to get your development setup ready. 
+ I personally used Visual Studio IDE to develop and test this application, [Quickstart: Create your first function in Azure using Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio) is a nice step-by-step guide for the same.


