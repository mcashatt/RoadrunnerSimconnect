# RoadrunnerSimconnect
A handy interface for extending Simconnect to a tactile flight control panel.  This repository contains a .NET solution with C# code for listening to events and reading variables from Simconnect (FSX, Prepar3D, etc) and then sending those values to Arduino so that you can make your own home cockpit.

There is also an Arduino folder containing sketches for the hardware.

#Done so far:
- Fuel Gauges.
 

#Could use some work:
- ReadVariables() loop.  This works well, but I have noticed sometimes that the Arduino seems not to respond.  Pressing the reset button seems to solve the problem, but that is not a reliable solution.

 
Thanks in advance for any contributions!

MC
