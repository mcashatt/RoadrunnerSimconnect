//
//
// https://msdn.microsoft.com/en-us/library/cc526980.aspx
//
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.FlightSimulator.SimConnect;

// Add these two statements to all SimConnect clients

namespace Simconnect_test
{
    public partial class Form1 : Form
    {
        public ArduinoControllerMain Acm;

        public bool LedOn;

        private void btnTestSerial_Click(object sender, EventArgs e)
        {
            LedOn = !LedOn;
            Acm.SendValue(LedOn ? "on" : "off");
        }

        #region Structs

        // This is how you declare a data structure so that 
        // simconnect knows how to fill it/read it. 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct Struct1
        {
            // this is how you declare a fixed size string 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public readonly string title;
            public readonly double latitude;
            public readonly double longitude;
            public readonly double altitude;
            public readonly double heading;
            public readonly double gearPosition;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly string atcId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly string DestinationAirport;

            //Fuel
            public readonly double fuelCenter;
            public readonly double fuelCenter2;
            public readonly double fuelCenter3;
            public readonly double fuelLeftMain;
            public readonly double fuelLeftAux;
            public readonly double fuelLeftTip;
            public readonly double fuelRightMain;
            public readonly double fuelRightAux;
            public readonly double fuelRightTip;
            public readonly double fuelExternal1;
            public readonly double fuelExternal2;
        }

        #endregion Structs

        #region Enums

        private enum SimEvents
        {
            AileronsLeft,
            AileronsRight,
            ApAltVarSetEnglish, //Sets altitude for autopilot
            ApAprHoldOn,
            ApAprHoldOff,
            ApAltHoldOn,
            ApAltHoldOff,
            ApAttHoldOn,
            ApAttHoldOff,
            ApHdgHoldOn,
            ApHdgHoldOff,
            ApLocHoldOn,
            ApLocHoldOff,
            ApMaster, //Toggles autopilot
            ApPanelAltitudeHold,
            ApPanelHeadingHold,
            AutopilotOff,
            AutopilotOn,
            Barometric,
            Brakes,
            BrakesLeft,
            BrakesRight,
            DecreaseThrottle,
            ElevDown,
            ElevUp,
            ElevTrimDn,
            ElevTrimUp,
            FlapsDecr,
            FlapsDown,
            FlapsIncr,
            FlapsUp,
            GearUp,
            GearDown,
            GearSet,
            GearToggle,
            GearPump,
            IncreaseThrottle,
            LandingLightDown,
            LandingLightHome,
            LandingLightLeft,
            LandingLightRight,
            LandingLightsOff,
            LandingLightsOn,
            LandingLightsSet,
            LandingLightsToggle,
            LandingLightUp,
            ParkingBrakes,
            PauseToggle,
            PitotHeatOff,
            PitotHeatOn,
            PitotHeatSet,
            PitotHeatToggle,
            SpoilersOff,
            SpoilersOn,
            SpoilersToggle,
            ThrottleCut,
            ThrottleDecr,
            ThrottleDecrSmall,
            ThrottleFull,
            ThrottleIncr,
            ThrottleIncrSmall,
            ThrottleSet
        }

        private enum NotificationGroups
        {
            Group0
        }

        private enum Definitions
        {
            Struct1
        }

        private enum DataRequests
        {
            Request1
        }

        #endregion Enums

        #region Methods

        #region Initialize & Connect

        public Form1()
        {
            InitializeComponent();

            SetButtons(true, false);

            //Create a new instance of the Arduino controller and connect.
            Acm = new ArduinoControllerMain();
        }

        private void ReadVariables()
        {
            simconnect.RequestDataOnSimObjectType(DataRequests.Request1, Definitions.Struct1, 0,
                SIMCONNECT_SIMOBJECT_TYPE.USER);
            Thread.Sleep(550);
        }

        // Simconnect client will send a win32 message when there is 
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WmUserSimconnect)
            {
                if (simconnect != null)
                {
                    simconnect.ReceiveMessage();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        private void SetButtons(bool bConnect, bool bDisconnect)
        {
            buttonConnect.Enabled = bConnect;
            buttonDisconnect.Enabled = bDisconnect;
        }

        private void CloseConnection()
        {
            if (simconnect != null)
            {
                // Dispose serves the same purpose as SimConnect_Close()
                simconnect.Dispose();
                simconnect = null;
                DisplayText("Connection closed");
            }
        }

        // Set up all the SimConnect related event handlers
        private void InitClientEvent()
        {
            try
            {
                #region Listeners

                //Listen to connect and quit msgs
                simconnect.OnRecvOpen += simconnect_OnRecvOpen;
                simconnect.OnRecvQuit += simconnect_OnRecvQuit;

                //Listen to exceptions
                simconnect.OnRecvException += simconnect_OnRecvException;

                //Listen to events
                simconnect.OnRecvEvent += simconnect_OnRecvEvent;

                //Listen to simobject data requests
                simconnect.OnRecvSimobjectDataBytype += simconnect_OnRecvSimobjectDataBytype;

                #endregion Listeners

                #region Event Subscriptions

                foreach (var simEvent in Enum.GetValues(typeof(SimEvents)))
                {
                    var eventName = GetEventName((Enum)simEvent);
                    var simconnectEventName = GetSimconnectNameFormat(eventName);
                    SubscribeToEvent((Enum)simEvent, simconnectEventName);
                }

                #endregion Event Subscriptions

                //Set the group priority
                simconnect.SetNotificationGroupPriority(NotificationGroups.Group0,
                    SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);

                #region Add Definitions

                simconnect.AddToDataDefinition(Definitions.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f,
                    SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Plane Latitude", "degrees",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Plane Longitude", "degrees",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Plane Altitude", "feet",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Plane Heading Degrees Magnetic", "degrees",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Gear Position", "enum", SIMCONNECT_DATATYPE.FLOAT64,
                    0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "ATC ID", null, SIMCONNECT_DATATYPE.STRING32, 0.0f,
                    SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Gps Approach Airport Id", null,
                    SIMCONNECT_DATATYPE.STRING32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                //FUEL
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Center Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Center2 Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Center3 Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Left Main Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Left Aux Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Left Tip Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Right Main Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Right Aux Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank Right Tip Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank External1 Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(Definitions.Struct1, "Fuel Tank External2 Level", "percent",
                    SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);


                // IMPORTANT: register it with the simconnect managed wrapper marshaller 
                // if you skip this step, you will only receive a uint in the .dwData field. 
                simconnect.RegisterDataDefineStruct<Struct1>(Definitions.Struct1);

                #endregion Add Definitions

                ReadVariables();
            }
            catch (COMException ex)
            {
                DisplayText(ex.Message);
            }
        }

        private void SubscribeToEvent(Enum simEvent, string simconnectEventName)
        {
            simconnect.MapClientEventToSimEvent(simEvent, simconnectEventName);
            simconnect.AddClientEventToNotificationGroup(NotificationGroups.Group0, simEvent, false);

            //Now add to EventModels
            var eventName = Enum.GetName(typeof(SimEvents), simEvent);
            EventModels.Add(new EventModel
            {
                DisplayMessage = SplitCamelCase(eventName),
                SimEventName = eventName,
                SimconnectName = simconnectEventName
            });
        }

        #endregion Initialize & Connect

        #region Simulator Events

        private void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            var eventName = GetEventName(recEvent.uEventID);
            var simEvent = GetEventModelByName(eventName);
            DisplayText(simEvent.DisplayMessage);
            
        }

        private void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            switch ((DataRequests)data.dwRequestID)
            {
                case DataRequests.Request1:
                    var s1 = (Struct1)data.dwData[0];

                    DisplayText("Title: " + s1.title);
                    DisplayText("Lat:   " + s1.latitude);
                    DisplayText("Lon:   " + s1.longitude);
                    DisplayText("Alt:   " + s1.altitude);
                    DisplayText("Heading:   " + s1.heading);
                    DisplayText("Gear Position:   " + s1.gearPosition);
                    DisplayText("AtcId:   " + s1.atcId);
                    DisplayText("Destination:   " + s1.DestinationAirport);
                    DisplayText("Fuel Left Wing:   " + s1.fuelLeftMain);
                    DisplayText("Fuel Center:   " + s1.fuelCenter);
                    DisplayText("Fuel Right Wing:   " + s1.fuelRightMain);
                    DisplayText("Fuel Aux:   " + s1.fuelExternal1);

                    var fuelCenter = (int) s1.fuelCenter;

                    //Debug.WriteLine(fuelCenter.ToString());
                    
                    var t = new Task(() => Acm.SendValue(fuelCenter.ToString()));
                    t.Start();

                    //Refresh variables
                    ReadVariables();

                    break;

                default:
                    DisplayText("Unknown request ID: " + data.dwRequestID);
                    break;
            }
        }

        private void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            DisplayText("Connected to FSX");
        }

        // The case where the user closes FSX
        private void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            DisplayText("FSX has exited");
            CloseConnection();
        }

        private void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            DisplayText("Exception received: " + data.dwException);
        }

        #endregion Simulator Events

        #region From UI Panel

        #endregion From UI Panel

        #region From Windows UI

        // The case where the user closes the client
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseConnection();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (simconnect == null)
            {
                try
                {
                    Acm.SetComPort();

                    // the constructor is similar to SimConnect_Open in the native API
                    simconnect = new SimConnect("Managed Client Events", Handle, WmUserSimconnect, null, 0);

                    SetButtons(false, true);

                    InitClientEvent();
                }
                catch (COMException ex)
                {
                    DisplayText("Unable to connect to FSX " + ex.Message);
                }
            }
            else
            {
                DisplayText("Error - try again");
                CloseConnection();

                SetButtons(true, false);
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            CloseConnection();
            SetButtons(true, false);
        }

        private void DisplayText(string s)
        {
            // remove first string from output
            _output = _output.Substring(_output.IndexOf("\n") + 1);

            // add the new string
            _output += "\n" + _response++ + ": " + s;

            // display it
            richResponse.Text = _output;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            simconnect.TransmitClientEvent(0, SimEvents.PauseToggle, 0, NotificationGroups.Group0,
                SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            simconnect.RequestDataOnSimObjectType(DataRequests.Request1, Definitions.Struct1, 0,
                SIMCONNECT_SIMOBJECT_TYPE.USER);
            DisplayText("Request sent...");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            simconnect.TransmitClientEvent(0, SimEvents.FlapsDecr, 0, NotificationGroups.Group0,
                SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var altitude = Convert.ToUInt32(textBox1.Text);
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, SimEvents.ApAltVarSetEnglish,
                    altitude,
                    NotificationGroups.Group0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (Exception)
            {
            }
        }

        #endregion From Windows UI

        #region Utility

        public EventModel GetEventModelByName(string eventName)
        {
            return EventModels.FirstOrDefault(f => f.SimEventName == eventName);
        }

        public string GetEventName(uint simEvent)
        {
            return Enum.GetName(typeof(SimEvents), simEvent);
        }

        public string GetEventName(Enum simEvent)
        {
            return Enum.GetName(typeof(SimEvents), simEvent);
        }

        public string SplitCamelCase(string s)
        {
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            return r.Replace(s, " ");
        }

        public string GetSimconnectNameFormat(string s)
        {
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            return r.Replace(s, "_").ToUpper();
        }

        #endregion Utility

        #endregion Methods

        #region Properties

        // User-defined win32 event
        private const int WmUserSimconnect = 0x0402;

        // Output text - display a maximum of 10 lines
        private string _output = "\n\n\n\n\n\n\n\n\n\n";

        // Response number
        private int _response = 1;

        // SimConnect object
        private SimConnect simconnect;

        public List<EventModel> EventModels = new List<EventModel>();

        public class EventModel
        {
            public string SimEventName { get; set; }
            public string SimconnectName { get; set; }
            public string DisplayMessage { get; set; }
        }

        #endregion Properties

        #region Variables

        public bool GearUp = false;
        public bool LandingLights = false;

        #endregion Variables
    }

    #region Arduino

    public class ArduinoControllerMain
    {
        public ArduinoControllerMain Adm;
        public SerialPort CurrentPort;
        public bool PortFound = false;

        public void SetComPort()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    //CurrentPort = new SerialPort(port, 9600);

                    if (!PortFound)
                    {
                        CurrentPort = new SerialPort(port, 115200);

                        var buffer = new List<byte>();
                        buffer.AddRange(Encoding.ASCII.GetBytes("handshake"));
                        buffer.Add(Convert.ToByte(4));

                        var bufferArray = buffer.ToArray();

                        CurrentPort.Open();

                        var t = new Thread(Handshake);
                        t.Start(CurrentPort);

                        CurrentPort.Write(bufferArray, 0, bufferArray.Length);

                        Thread.Sleep(1000);
                    }
                    
                }
            }
            catch (Exception e)
            {
            }
        }
        
        private void Handshake(object context)
        {
            SerialPort serialPort = context as SerialPort;

            while (serialPort.IsOpen)
            {
                try
                {
                    if(serialPort.BytesToRead > 0)
                    {
                        string inData = serialPort.ReadLine();
                        if (inData.Contains("HELLO FROM ROADRUNNER")) //Ensure we have connected to the right device.
                        {
                            PortFound = true;
                            Debug.WriteLine("PORT FOUND: " + serialPort.PortName);
                            serialPort.Close();
                        }
                        else
                        {
                            PortFound = false;
                            serialPort.Close();  
                        }
                    }
                }
                catch (Exception ex)
                {
                    var x = ex.Message;
                    PortFound = false;
                    serialPort.Close();
                }
            }
        }

        private void ProcessMessage(object context)
        {
            SerialPort serialPort = context as SerialPort;

            while (serialPort.IsOpen)
            {
                try
                {
                    string inData = serialPort.ReadLine();
                    if (inData.Contains("MESSAGE RECEIVED"))
                    {
                        Debug.WriteLine(inData);
                    }
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    var x = ex.Message;
                }
            }
        }

        public void SendValue(string msg)
        {
            if (!PortFound)
            {
                SetComPort();
            }
            else if(!CurrentPort.IsOpen)
            {
                try
                {
                    Debug.WriteLine("SENDING MESSAGE ON: " + CurrentPort.PortName);

                    CurrentPort.Open();

                    var buffer = new List<byte>();
                    buffer.AddRange(Encoding.ASCII.GetBytes(msg));
                    buffer.Add(Convert.ToByte(4));

                    var bufferArray = buffer.ToArray();
                    
                    var t = new Thread(ProcessMessage);
                    t.Start(CurrentPort);
                    CurrentPort.Write(bufferArray, 0, bufferArray.Length);
                }
                catch (Exception ex)
                {
                    var x = ex.Message;
                }
            }
        }
    }

    #endregion Arduino
}

// End of sample