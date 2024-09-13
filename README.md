# UXF Replay Engine

A replay engine to play tracker data collected from UXF. Relies on [UXF extensions](https://github.com/ovi-lab/UXF-extensions).

## Installation

### ⚠ Attention ⚠

**To use this project, you must have [UXF Extensions](https://github.com/ovi-lab/UXF-extensions) installed first!**

If you do not have UXF Extensions installed already, a dialogue box will request you to install UXF Extensions after you install the Replay Engine.

UXF Replay Engine can be added to your project via [Git UPM](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html).
To add this package, copy:

```shell
https://github.com/ovi-lab/UXF-ReplayEngine.git
```

and add it as a git package in the Unity Package Manager

![Add_Package_Via_Git](https://github.com/ovi-lab/UXF-ReplayEngine/blob/main/Documentation~/add_package_from_git.png)

## Usage

### Setup

- Create a copy of the scene with your UXF setup, and rename it.
- Remove your Experiment Manager, and the UXF Rig.
  - **KEEP YOUR TRACKERS INTACT!** This will be used to inform the engine which objects to replay.
- Create a new GameObject, and attach the `Replay Engine` component to it.

### Loading Data

In Unity Editor:
-  Set the target participant id, session, and/or trial data file. Leaving any of them blank will load the first available folder or file.
- Press the `Select Path and Load Data` button. Navigate to the folder which contains the different participant data.

Via Scripts:
- Get a reference to the Replay Engine, and call the `Load Data` Function. Set the parameters as per the function XML documentation. [TODO: Set up proper documentation]


### Known Issues

If the trial is too long and the amount of data being loaded is too much, Unity will severely lag. This is also dependent on your compute power. Might try to parallel process the tracker data in the future to avoid this.

## Contributing

Please see the [CONTRIBUTING.md](CONTRIBUTING.md) file for details on contributing to this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
