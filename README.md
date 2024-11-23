![logo](robosharp.png?raw=true)
# RoboSharp
![AppVeyor Build](https://img.shields.io/appveyor/build/tjscience/RoboSharp)
### NuGet Packages
![Nuget](https://img.shields.io/nuget/v/RoboSharp?label=RoboSharp&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharp) 
![Nuget](https://img.shields.io/nuget/dt/RoboSharp?label=%20&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharp)\
![Nuget](https://img.shields.io/nuget/v/RoboSharp.Extensions?label=RoboSharp.Extensions&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharp.Extensions)
![Nuget](https://img.shields.io/nuget/dt/RoboSharp.Extensions?label=%20&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharp.Extensions)\
![Nuget](https://img.shields.io/nuget/v/RoboSharpNET35?label=RoboSharpNET35&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharpNET35)
![Nuget](https://img.shields.io/nuget/dt/RoboSharpNET35?label=%20&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRoboSharpNET35)

### Wiki - in progress - https://github.com/PCAssistSoftware/RoboSharp/wiki

## About

RoboSharp is a .NET wrapper for the awesome Robocopy windows application.

Robocopy is a very extensive file copy application written by microsoft and included in modern versions of Windows. To learn more about Robocopy, visit the documentation page at http://technet.microsoft.com/en-us/library/cc733145.aspx.

RoboSharp came out of a need to manipulate Robocopy in a c# backup application that I was writing. It has helped me tremendously so I thought that I would share it! It exposes all of the switches available in RoboCopy as descriptive properties. With RoboSharp, you can subscribe to events that fire when files are processed, errors occur and even as the progress of a file copy changes. Another really nice feature of RoboSharp is that you can pause and resume a copy that is in progress which is a feature that I though was lacking in Robocopy.

In the project, you will find the RoboSharp library as well as a recently updated sample backup application that shows off many (but not all) of the options. 

If you like the project, please rate it!

## Examples

See the [Wiki](https://github.com/PCAssistSoftware/RoboSharp/wiki) for examples and code snippets

## Bugs / Issues

Before submitting issues please ensure you are using the latest version available, this project is continuously being worked on and improved, so it maybe that a later version already resolve any problems you are having.  Thank you.

## Contributing to RoboSharp

First off, thanks! Please go through the [guidelines](CONTRIBUTING.md).

## Thanks

Many thanks go to [tjscience](https://github.com/tjscience) for originally creating this great project. And also thanks to [RFBomb](https://github.com/RFBomb) for all his recent contributions that have helped make great improvements.  Hopefully this project can continue improving for many years to come.
