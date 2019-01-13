This is simple utility for displaying Visual Studio project-projects dependencies in nodes/tree like view.

It was written to quickly figure out which nuget packages should be published and in what order as I didn't found any free option to do this.

![](.\doc\preview.png)

#### The program will

- Display project dependency order starting from specified project in dedicated window.
- Quickly save the graph to **.png** file using **ctrl+s** shortcut.

#### The program will not

- Show any no-project references i.e. directly linked .dll files or external nuget packages are not considered in the graph.
- Show multiple references to the same project - only first node reference will be showed. This is done intentionally to keep the graph clear but you should be able to modify the code easy to show all links.




The program was tested with **Visual Studio Community 2017** and **.csproj** projects but it is possible it will work with other setups.

### Quick setup

1. Download the solution

2. Compile **SolutionNodesLauncher**

3. Add output executable as external tool to your to Visual Studio

   1. **VS>Tools>External Tools>Add**

   2. In **Command** field browse to **SolutionNodesLauncher.exe**

   3. In **Arguments** field provide at least first two arguments:

      `$(SolutionFileName) $(ItemFileName)`

      To see what other arguments you can provide inspect **[Program](./SolutionNodesLauncher/Program.cs)** class of the launcher.

   4. Check **Use Output window**

   5. Remember the position of new tool in the list (lets call it **X**).

   6. Click **Apply** and **OK**

4. Add the tool to project item's context menu

   1. **VS>Tools>Customize...>Commands>Context menu**
   2. In combobox find **Project and Soultion context menus | Project | View**
   3. Click **Add Command** than **Tools>External Command>External Command X** where **X** is the position of the command you set before in **External Tools** window (those positions start from 1).

5. Now you should be able to display the graph from project's **context menu>view>your command name** in Solution Explorer.

   ​

### Configuration for other Visual Studio versions

The program is configured by default for **VS Community 2017** and will not work for other versions (like **Express** edition) without additional configuration.

To access Visual Studio instance the program scans your system to find VS process. Unfortunately different versions of VS could have different process names. The VS object also have different name which is also needed for getting access to it.

You can change default configuration by specifying the values as arguments to **SolutionNodesLauncher** **[Program](./SolutionNodesLauncher/Program.cs)**. The variables are `VSProces` and `VSName`.

For VS Community 2017 it is `devenv` and `!VisualStudio` for process and object name respectively. For Express version of VS it will be `WDExpress` and `!WDExpress` (from what I can recall). You can find the process name for your VS in windows Task Manager. For object name, the program will display possible values in **Output**  view of visual studio.



### About the source

The program uses **[NodeNetwork](https://github.com/Wouterdek/NodeNetwork)** library to display the nodes.

It also uses modified version of **[VisualStudioAttacher](https://gist.github.com/atruskie/3813175)** to get access to visual studio instance.

The solution is splitted into 3 little projects (other are just empty projects for testing):

- **SolutionScan** - For traversing a solution.
- **SolutionNodes** -  WPF application for displaying results as nodes.
- **SolutionNodesLanucher** - Console application which opens nodes window based on specified arguments so that the program can be configured as VS external tool.