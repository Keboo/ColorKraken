# ColorKraken



A simple WPF application for editing [GitKraken](https://gitkraken.keboo.dev) themes.
![gitkraken-logo-light-hz](https://user-images.githubusercontent.com/952248/165584032-96d5badb-3f9c-4a28-b118-014419c80d3e.svg)


[GitKraken v8.2.0](https://support.gitkraken.com/release-notes/current/#version-820) introduced custom color themes. This simple desktop applications allows for easy editing of those themes. 


## Installation
You can download pre-compiled versions of the application from the latest [Release](https://github.com/Keboo/ColorKraken/releases).
Alternatively you can install using [chocolatey](https://community.chocolatey.org/packages/colorkraken)
```ps
choco install colorkraken
```

## About

Though the color picker on each theme color defaults to an RGB value, the theming in GitKraken supports more advanced CSS functions (such as lighten(), fade(), etc). Check out the existing themes to get an idea of what is possible. 

Once you create a theme, in GitKraken, open Preferences >> UI Customization and select your new theme from the Theme drop down. Once selected changes you make in this app will appear immediately in GitKraken. Check out changing `app__bg0` to change the main application background color for a very noticiple change.

![image](https://user-images.githubusercontent.com/952248/147212439-57529a9e-0f0e-4177-9941-ed7e1bc741b6.png)

## Local Chocolately Package Testing
You can download the NuGet package that would be pushed to chocolatey and test it locally. Because it takes a dependency on the dotnet 6 desktop runtime you will need to run the following command from the directory where the downloaded nupkg is located. If it is a pre-release package you will also need to specify `--pre`

```ps
choco install colorkraken -dv -source "'.;https://community.chocolatey.org/api/v2/'" --pre
```
