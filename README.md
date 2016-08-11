# RoadrunnerSimconnect
A handy interface for extending Simconnect to a tactile flight control panel.  This repository contains a .NET solution with C# code for listening to events and reading variables from Simconnect (FSX, Prepar3D, etc) and then sending those values to Arduino so that you can make your own home cockpit.

There is also an Arduino folder containing sketches for the hardware.

#Done so far:
- Fuel Gauges.
 

#Could use some work:
- Event & variable reading loop.  Currently, a windows form pops up and I actually click a button to connect to FSX and read the variables.  This is fine for now, but would be thrilled to have some help making that a continuous but efficient loop.

 
Thanks in advance for any contributions!

MC
