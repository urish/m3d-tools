# m3d-tools
Command line tools for interacting with the M3D Printer, by Uri Shaked.

## About

This distribution contains two command line tools for M3D Printer users:

* Processor - Prepares your GCode files for the M3D Printer
* PrintGCode - Prints a prepared GCode file

You can use these tools in Mac/Linux by installing [Mono](http://www.mono-project.com/). The Processor was tested under Linux with Mono, and should run without any issues.

Hopefully, the existence of these tools will set the ground for a better integration between M3D and open-source slicer software.

Important: These tools are not officially supported! Use at your own risk!

## Installation

You need to install the official M3D software first. The supported version is `2015-07-15-V1.3.5.0`. After installing the software, copy the following files into the `Dist` folder:

* Micro3DSpooler.exe
* Micro3DSpooling.dll

Then, edit the `Processor.exe.config` and change the settings to match your printer's backlash settings and bed orientation. Those values can be obtained via the M3D GUI.
You also need to set the printing material (PLA/ABS/HIPS) and printing temperature.

## Running the Processor

The processor takes two parameters: an input file and an output file. The input file should be generated by a slicer software, such as [Cura](https://ultimaker.com/en/products/cura-software), and the output will contain the processed GCode that can be fed to the printer.

Example:

`Processor.exe input.gcode output.gcode`

## Running the PrintGCode utility

The PrintGCode utility takes two parameters: The name of the USB serial port that the printer is connected to (e.g. COM15), and the name of the 
processed GCode file to print. The printer must be in Firmware mode, as the program currently does not handle the Boot-loader mode.

Example:

`PrintGCode.exe COM15 output.gcode`

Note: if you stop the process while printing, you will have to manually turn off the heater, fan and motors.

## Contributions:

Pull requests are welcome!
