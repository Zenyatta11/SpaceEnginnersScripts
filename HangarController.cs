static String gridId;
const int VERSION = 2;

List<IMyDoor> hangarDoors = new List<IMyDoor>();
List<IMyDoor> innerDoors = new List<IMyDoor>();
List<IMyInteriorLight> innerLights = new List<IMyInteriorLight>();
List<IMyInteriorLight> outerLights = new List<IMyInteriorLight>();

IMyShipConnector connector;
IMyAirVent sensor;

bool hangarOpen = true;
bool pressurized = false;
bool shipConnected = false;
bool doorsOpened = false;
bool lightsDoorMoving = false;
string errors = "";

int tick = 0;
int lightDebounce = 0;
int doorDebounce = -1;

Color initialColor;

string status = "Initial";
string hangarName;

public Program()
{
    gridId = Me.CubeGrid.ToString();
    hangarName = Me.CustomData;

    if (hangarName == "")
    {
        Log("No block group set in Custom Data");
        return;
    }

    IMyBlockGroup hangarBlockGroup = GridTerminalSystem.GetBlockGroupWithName(hangarName);
    if (hangarBlockGroup == null)
    {
        Log("No block group named 'Hangar_" + hangarName);
        return;
    }

    List<IMyShipConnector> connectors = new List<IMyShipConnector>();
    List<IMyAirVent> vents = new List<IMyAirVent>();

    hangarBlockGroup.GetBlocksOfType(hangarDoors, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("HangarDoor"));
    hangarBlockGroup.GetBlocksOfType(innerDoors, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("InnerDoor"));
    hangarBlockGroup.GetBlocksOfType(outerLights, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("Outer_Light"));
    hangarBlockGroup.GetBlocksOfType(innerLights, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("Inner_Light"));
    hangarBlockGroup.GetBlocksOfType(connectors, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    hangarBlockGroup.GetBlocksOfType(vents, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    
    if(!hangarDoors.Any()) errors += "\n\tNo hangar doors in group '" + hangarName + "'";
    if(!innerDoors.Any()) errors += "\n\tNo inner doors in group '" + hangarName + "'";
    if(!outerLights.Any()) errors += "\n\tNo outer lights in group '" + hangarName + "'";
    if(!innerLights.Any()) errors += "\n\tNo inner lights in group '" + hangarName + "'";
    if(!connectors.Any()) errors += "\n\tNo connectors in group '" + hangarName + "'";
    if(!vents.Any()) errors += "\n\tNo air vents in group '" + hangarName + "'";

    if(errors != "") {
        Log(errors);
        return;
    }

    connector = connectors.ElementAt(0);
    shipConnected = connector.IsConnected;

    sensor = vents.ElementAt(0);
    pressurized = (sensor.GetOxygenLevel() > 0.5);

    foreach (IMyDoor item in innerDoors)
    {
        item.CloseDoor();
        item.Enabled = (sensor == null || pressurized);
        doorsOpened = (sensor == null || pressurized);
    }

    foreach (IMyInteriorLight item in innerLights)
    {
        item.Enabled = true;
        initialColor = item.Color;
    }

    foreach (IMyInteriorLight item in outerLights)
    {
        item.Enabled = true;
    }


    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource)
{
    if(errors != "") return;

    tick = tick + 1;
    if (tick > 3) tick = 0;

    pressurized = (sensor.GetOxygenLevel() > 0.5);
    bool doorsMoving = !(hangarDoors.ElementAt(0).OpenRatio == 0 || hangarDoors.ElementAt(0).OpenRatio == 1);

    foreach(IMyDoor door in innerDoors) {
        if(door.Enabled) doorsOpened = true;
    }

    if(doorsMoving && !lightsDoorMoving) {
        lightsDoorMoving = true;
        doorMovingLights(innerLights);
    } else if(!doorsMoving && lightsDoorMoving) {
        lightsDoorMoving = false;
        resetLightColor(innerLights);
    }

    if (doorDebounce > 0) {
        doorDebounce = doorDebounce - 1;

        if (doorDebounce == 0) {
            doorsOpened = pressurized;
            enableDoors(innerDoors, pressurized);
        }
    }

    if (connector.IsConnected != shipConnected)
    {
        shipConnected = connector.IsConnected;
        lightDebounce = 60;

        if (shipConnected) onConnect();
        else onDisconnect();

        toggleLights(innerLights, true);
        toggleLights(outerLights, true);
    }

    if (pressurized && !doorsOpened) {
        doorsOpened = true;
        enableDoors(innerDoors, true);
    } else if (!pressurized && doorsOpened && doorDebounce <= 0) {
        doorDebounce = 5;
        doorsOpened = false;
        toggleDoors(innerDoors, false);
    }

    String textToShow = "Version " + VERSION + " of Hangar Program - " + getWorkingAnimation();
    textToShow = textToShow + "\n\tStatus - " + status;
    textToShow = textToShow + "\n\tPressurized: " + (pressurized ? "Yes" : "No");
    textToShow = textToShow + "\n\tHangar Open: " + (doorsMoving ? "Moving..." : (hangarOpen ? "Yes" : "No"));
    textToShow = textToShow + "\n\tShip docked: " + (shipConnected ? "Yes" : "No");
    textToShow = textToShow + "\n\tDoors Open: " + (doorsOpened ? "Yes" : "No");
    textToShow = textToShow + "\n\tDoor debounce: " + doorDebounce;
    textToShow = textToShow + "\n\tDoor ratio: " + hangarDoors.ElementAt(0).OpenRatio;
    textToShow = textToShow + "\n\tOxygen level: " + sensor.GetOxygenLevel();
    textToShow = textToShow + "\n\tcxycy: " + (!pressurized && doorsOpened ? "true" : "false");

    Log(textToShow);

    if (lightDebounce > 0)
    {
        lightDebounce = lightDebounce - 1;

        if (lightDebounce == 0)
        {
            if (shipConnected)
            {
                status = "Hangar Closed";
                enableDoors(innerDoors, true);
                toggleLights(outerLights, false);
            }
            else
            {
                status = "Hangar Opened";
                enableDoors(innerDoors, false);
                toggleLights(innerLights, false);
            }
        }
    }
}

void onConnect()
{
    status = "Hangar Closing";
    toggleDoors(hangarDoors, false);
}

void onDisconnect()
{
    status = "Hangar Opening";
    toggleDoors(hangarDoors, true);
    toggleDoors(innerDoors, false);
}

void toggleDoors(List<IMyDoor> doors, bool open)
{
    foreach (IMyDoor item in doors)
    {
        if (open) item.OpenDoor();
        else item.CloseDoor();
    }
}

void enableDoors(List<IMyDoor> doors, bool on)
{
    if (sensor == null) return;

    bool valueToSet = on && sensor.GetOxygenLevel() > 0.5;

    foreach (IMyDoor item in doors)
    {
        item.Enabled = valueToSet;
    }

}

void toggleLights(List<IMyInteriorLight> lights, bool on)
{
    foreach (IMyInteriorLight item in lights)
    {
        item.Enabled = on;
    }
}

void doorMovingLights(List<IMyInteriorLight> lights)
{
    foreach (IMyInteriorLight item in lights)
    {
        item.Enabled = true;
        
        item.Intensity = 2;
        item.Color = Color.Orange;
        item.BlinkLength = 50F;
        item.BlinkIntervalSeconds = 2;
    }
}

void resetLightColor(List<IMyInteriorLight> lights) {
foreach (IMyInteriorLight item in lights)
    {
        item.Enabled = connector.IsConnected;

        item.Intensity = 10;
        item.Color = initialColor;
        item.BlinkLength = 0F;
        item.BlinkIntervalSeconds = 0;
    }
}

string getWorkingAnimation()
{
    switch (tick)
    {
        case 0: return "|";
        case 1: return "/";
        case 2: return "-";
        case 3: return "\\";
        default: return "x";
    }
}

void Log(string text)
{
    Me.GetSurface(0).WriteText(text, false);
}
