---
page_type: sample
languages:
- csharp
- powershell
products:
- azure
- azure-active-directory
- dotnet
- aspnet
- ms-graph
description: "Learn how to acquire an access token on a console application."
urlFragment: "ms-identity-dotnet-desktop-tutorial"
---

# Acquiring an access token using a console application and call APIs with the Microsoft identity platform for developers

## About this sample

A multi-target console application (.Net Core and .Net Framework) that acquires an access token for a protected API on Azure, using Microsoft identity platform for developers. There are steps demonstrating this scenario on Azure AD, Azure AD B2C and National Clouds.

On later steps, you will learn how to enrich the console application with a cross platform token cache and a custom Web UI (for .NET Core only).

## Structure of the repository

This repository contains a progressive tutorial made of the following parts:

| Sub folder                       | Description                      |
| -------------------------------- | -------------------------------- |
| [1. Calling Microsoft Graph](https://github.com/Azure-Samples/ms-identity-dotnet-desktop-tutorial/tree/master/1-Calling-MSGraph) | This first part presents how to acquire an access token for Microsoft Graph, on Azure AD, Azure B2C and Azure National Clouds. Each scenario is separated on its correspondent sub-folder.|
| [2. Cross platform token cache](https://github.com/Azure-Samples/ms-identity-dotnet-desktop-tutorial/tree/master/2-TokenCache) | This step shows how to configure a cross platform token cache (Windows, Linux and MAC) leveraging `Microsoft.Identity.Client.Extensions.Msal` |

## Prerequisites

- Install .NET Core for Windows by following the instructions at [dot.net/core](https://dot.net/core).
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- A user account in your Azure AD tenant.

## Setup

From your shell or command line:

```Shell
git clone https://github.com/Azure-Samples/ms-identity-dotnet-desktop-tutorial.git dotnet-desktop-tutorial
cd dotnet-desktop-tutorial
```

> Given that the name of the sample is pretty long, that it has sub-folders and so are the name of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/msal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`msal` `dotnet`].

If you find a bug in the sample, please open an issue on [GitHub Issues](https://github.com/Azure-Samples/ms-identity-dotnet-desktop-tutorial/issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
