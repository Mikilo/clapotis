NG Package Helpers


NG Package Deployer

Requirements:
None

NG Package Deployer is a handy tool to deploy your package over many Unity projects and compile them in just one click.

There is 2 steps to use it.

1 - Detect Unity installations
	1 - Click on "Add".
	2 - Write path of folders where Unity installations are installed.
	On Windows, it is most of the time in "C:\Program Files (x86)" for Unity 4 and "C:\Program Files" for Unity 5.

Your Unity installations folder must respect the following naming:

"Unity A.B.C[abfpx]NN"

Examples:
C:\Program Files (x86)\Unity 4.3.2f2
C:\Program Files (x86)\Unity4.5.0p5
C:\Program Files\Unity5.4.0b19

The prefix "Unity" is not important, the important part is the version!


2 - Detect Unity projects
	1 - Click on "Detect Projects"
	Select a folder where reside Unity projects you want to deploy in.

Your Unity projects must respect the following naming:

"Package A.B.C[abfpx]NN"

Examples:
C:\Package\Package4.3.2f2
C:\Package\Something 4.5.0p5
C:\Package\Dummy5.4.0b19

Again, the important part is the version!

3 - Deploy
If Unity installations and projects are correctly detected, button "Deploy" should be enable.
Otherwise, move your mouse over the button "Deploy" to display the error.

Click on "Deploy" to deploy over a specific project.
Click on "Deploy All" to deploy over all the projects.

Deploying in a project will copy your source files and your resources regarding keywords from NG Package Excluder.



NG Package Exporter

Requirements:
None

NG Package Exporter generates a package with your assets.

The first button "Export Package" will generate a package with assets filtered.
Assets are filtered regarding keywords from NG Package Excluder.

The button "Full Export Package" will generate a package with all your assets.
Useful to export a "dev" version.

Also, NGPackageExporter has 2 events BeforeEvent and GetVersion.

BeforeEvent is invoked just before exporting a filtered package.
Use it to process stuff when exporting.
In my case, I use it to automatically update my version number using UnityEditor.AssetModificationProcessor.

GetVersion is called in OnEnable to have a predefined version.
Use it to set your own versioning system.



NG DLL Generator

Requirements:
Windows
Visual Studio

NG DLL Generator copies your package in Visual Studio projects and compiles them into DLLs for UnityEngine and UnityEditor.

Pass your mouse over any fields to display a tooltip with help inside.

1 - Create Unity project
This project will receive the generated DLLs and your package's resources.
Set "Unity Project" in NG DLL Generator.

2 - Create Visual Studio projects
Create one library project for your package for UnityEngine.
Create one library project for your package for UnityEditor.
Respectively set "Project Path" and "Project Editor Path" with your new projects in NG DLL Generator.

See http://docs.unity3d.com/Manual/UsingDLL.html

3 - Generate
If everything is correctly assigned, there should be no warning displaying.
When clicking on "Generate DLL UnityEngine", it generates a DLL at {Unity Project}/Assets/Plugins/{PackageName}.dll
Clicking on "Generate DLL UnityEditor" will generate a DLL in Plugins/Editor.

The source files used by those projects are based on keywords from NG Package Excluder.



NG Package Excluder

NG Package Excluder is a companion tool used by NG Package Deployer, NG Package Exporter and NG DLL Generator.
Use it to exclude or include assets regarding keywords. It is useful to discard personal folders, internal stuff, etc...

Specifically for NG Package Deployer, you can force inclusion of assets outside your package folder.