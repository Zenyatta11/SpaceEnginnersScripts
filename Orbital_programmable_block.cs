const String shipName = "Orbital_1";
const bool isHydrogenShip = true;
const int weightLimit = 260000;

////// de aca para abajo no tocar \\\\\\

static Color lowOxygen = Color.DarkBlue;
static Color lowFuel = Color.Orange;
static Color lowPower = Color.Yellow;
static Color depressurized = Color.Red;
static String gridId;

List<IMyTextPanel> statusScreens = new List<IMyTextPanel>();
List<IMyLandingGear> landingGear = new List<IMyLandingGear>();
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMyShipConnector> connectors = new List<IMyShipConnector>();
List<IMyAirVent> airVents = new List<IMyAirVent>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();

List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
List<IMyGasTank> hydrogenTanks = new List<IMyGasTank>();

List<IMyDoor> innerDoors = new List<IMyDoor>();
List<IMyDoor> outerDoors = new List<IMyDoor>();

List<IMyInteriorLight> exteriorLights = new List<IMyInteriorLight>();
List<IMyInteriorLight> interiorLights = new List<IMyInteriorLight>();
List<IMyInteriorLight> statusLights = new List<IMyInteriorLight>();

List<IMyThrust> downThrusters = new List<IMyThrust>();
List<IMyThrust> upThrusters = new List<IMyThrust>();
List<IMyThrust> leftThrusters = new List<IMyThrust>();
List<IMyThrust> rightThrusters = new List<IMyThrust>();
List<IMyThrust> frontThrusters = new List<IMyThrust>();
List<IMyThrust> backThrusters = new List<IMyThrust>();
List<IMyThrust> allThrusters = new List<IMyThrust>();

IMyShipController controller;

float minThrust;
int tick = 0;
int outerDoorDisableTimer = 0;
bool hasOxygenTanks = false;

public Program() {
    gridId = Me.CubeGrid.ToString();
    GridTerminalSystem.GetBlocksOfType(statusScreens, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);

    controller = (IMyShipController)GridTerminalSystem.GetBlockWithName(shipName + "PilotSeat");
    if(controller == null) {
        Log("No pilot seat!");
        return;
    }

    GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(landingGear, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(connectors, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(airVents, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(cargoContainers, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);

    GridTerminalSystem.GetBlocksOfType(oxygenTanks, block => block.IsFunctional && block.CustomName.Contains("Oxygen") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(hydrogenTanks, block => block.IsFunctional && block.CustomName.Contains("Hydrogen") && block.CubeGrid.ToString() == gridId);

    GridTerminalSystem.GetBlocksOfType(innerDoors, block => block.IsFunctional && block.CustomName.Contains("Inner") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(outerDoors, block => block.IsFunctional && block.CustomName.Contains("Outer") && block.CubeGrid.ToString() == gridId);

    GridTerminalSystem.GetBlocksOfType(interiorLights, block => block.IsFunctional && block.CustomName.Contains("Int") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(exteriorLights, block => block.IsFunctional && block.CustomName.Contains("Ext") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(statusLights, block => block.IsFunctional && block.CustomName.Contains("Status") && block.CubeGrid.ToString() == gridId);

    GridTerminalSystem.GetBlocksOfType(downThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Up && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(upThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Down && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(leftThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Right && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(rightThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Left && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(frontThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Backward && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(backThrusters, block => block.IsFunctional && block.GridThrustDirection == Vector3I.Forward && block.CubeGrid.ToString() == gridId);

    allThrusters.AddRange(downThrusters);
    allThrusters.AddRange(upThrusters);
    allThrusters.AddRange(leftThrusters);
    allThrusters.AddRange(rightThrusters);
    allThrusters.AddRange(frontThrusters);
    allThrusters.AddRange(backThrusters);

    foreach(IMyThrust thruster in allThrusters) {
        thruster.Enabled = true;
    }

    minThrust = getTotalThrust(downThrusters);
    float upThrust = getTotalThrust(upThrusters);
    float leftThrust = getTotalThrust(leftThrusters);
    float rightThrust = getTotalThrust(rightThrusters);
    float frontThrust = getTotalThrust(frontThrusters);
    float backThrust = getTotalThrust(backThrusters);

    foreach(IMyInteriorLight statusLight in statusLights) {
        statusLight.Color = Color.Green;
    }

    if(upThrust < minThrust) minThrust = upThrust;
    if(leftThrust < minThrust) minThrust = leftThrust;
    if(rightThrust < minThrust) minThrust = rightThrust;
    if(frontThrust < minThrust) minThrust = frontThrust;
    if(backThrust < minThrust) minThrust = backThrust;

    bool isDocked = getIsDocked();
    bool isLanded = getIsLanded();

    if(isDocked) onDock();
    else if(isLanded) onLand();

    hasOxygenTanks = oxygenTanks.Any();
    
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource) {
    if(outerDoorDisableTimer == 1) {
        foreach(IMyDoor door in outerDoors) {
            door.Enabled = false;
        }
    }

    if(outerDoorDisableTimer > 0) {
        outerDoorDisableTimer = outerDoorDisableTimer - 1;
    }

    tick = tick + 1;
    if(tick > 9) tick = 0;

    int powerLevel = (int)getPowerLevel();
    int oxygenLevel = (int)getGasLevel(oxygenTanks);
    int fuelLevel = (int)getGasLevel(hydrogenTanks);
    //int cargoLevel = (int)getCargoLevel(); //TODO
    
    bool isDocked = getIsDocked();
    bool isLanded = getIsLanded();
    bool wasDocked = getWasDocked();
    bool wasLanded = getWasLanded();

    if(isDocked && !wasDocked) onDock();
    else if(!isDocked && wasDocked) onUndock();
    else if(isLanded && !wasLanded) onLand();
    else if(!isLanded && wasLanded) onTakeoff();

    String textToShow = "Version 2 of Program - " + tick;
    textToShow = textToShow + "\n\t[" + getFillLevel(powerLevel) + "] " + String.Format("{0,4}", powerLevel) + "% | " + String.Format("{0,8}", "Power");
    if(hasOxygenTanks) textToShow = textToShow + "\n\t[" + getFillLevel(oxygenLevel) + "] " +  String.Format("{0,4}", oxygenLevel) + "% | " + String.Format("{0,8}", "Oxygen");
    if(isHydrogenShip) textToShow = textToShow + "\n\t[" + getFillLevel(fuelLevel) + "] " +  String.Format("{0,4}", fuelLevel) + "% | " + String.Format("{0,8}", "Fuel");
    //textToShow = textToShow + "\n\t[" + getFillLevel(cargoLevel) + "] " +  String.Format("{0,16}", cargoLevel + "% | Cargo");
    textToShow = textToShow + "\n\tisDocked: " + (isDocked ? "1" : "0") + "; wasDocked: " + (wasDocked ? "1" : "0") + "; gravity: " + controller.GetNaturalGravity().Sum;
    textToShow = textToShow + "\n\tisAirtight:" + (notAirtight() ? "0" : "1") + "; thrustUsage: " + getThrusterLimitPercentage() + "%";
    textToShow = textToShow + "\n\tisInSpace:" + (amInSpace() ? "1" : "0") + "; minThrust: " + minThrust + "; totalMass: " + controller.CalculateShipMass().TotalMass + ";";
    textToShow = textToShow + "\n\tupThrust: " + getTotalThrust(upThrusters) + ";";

    String statusText = doStatusLights(powerLevel, oxygenLevel, fuelLevel);
    textToShow = textToShow + "\n" + statusText;

    Log(textToShow);
}

int getThrusterLimitPercentage() {
    if(weightLimit != 0) return 0;
    float totalMass = (float)controller.CalculateShipMass().TotalMass;
    
    return (int)(totalMass * 10F / minThrust * 100);
}

float getTotalThrust(List<IMyThrust> thrusters) {
    float thrust = 0;

    foreach(IMyThrust thruster in thrusters) {
        thrust = thrust + thruster.MaxThrust;
    }

    return thrust;
}

double getPowerLevel() {
    int maxCharge = 0;
    int currentCharge = 0;

    foreach(IMyBatteryBlock item in batteries) {
        maxCharge = maxCharge + getPower(item, true);
        currentCharge = currentCharge + getPower(item, false);
    }

    double percentage = ((double)currentCharge / (double)(maxCharge == 0 ? 1 : maxCharge));
    return percentage = Math.Floor(percentage * 100);
}

double getGasLevel(List<IMyGasTank> gasTanks) {
    int maxFluid = 0;
    int currentFluid = 0;

    foreach(IMyGasTank item in gasTanks) {
        maxFluid = maxFluid + (int)item.Capacity;
        currentFluid = currentFluid + (int)(item.FilledRatio * item.Capacity);
    }

    double percentage = ((double)currentFluid / (double)(maxFluid == 0 ? 1 : maxFluid));
    return percentage = Math.Floor(percentage * 100);
}

bool getIsDocked() {
    bool isDocked = false;

    foreach(IMyShipConnector connector in connectors) {
        if(connector.Status == MyShipConnectorStatus.Connected) isDocked = true;
    }

    return isDocked;
}


bool getIsLanded() {
    bool isDocked = false;

    foreach(IMyLandingGear gear in landingGear) {
        if(gear.IsLocked) isDocked = true;
    }

    return isDocked;
}

bool getWasDocked() {
    bool isDocked = false;

    foreach(IMyShipConnector connector in connectors) {
        if(connector.CustomData.Contains("WasDocked;")) isDocked = true;
    }

    return isDocked;
}

bool getWasLanded() {
    bool isDocked = false;

    foreach(IMyLandingGear gear in landingGear) {
        if(gear.CustomData.Contains("WasLanded;")) isDocked = true;
    }

    return isDocked;
}

void onLand() {
    foreach(IMyLandingGear connector in landingGear) {
        if(!connector.CustomData.Contains("WasLanded;")) connector.CustomData = connector.CustomData + "WasLanded;";
    }

    foreach(IMyThrust thruster in allThrusters) {
        thruster.Enabled = false;
    }

    foreach(IMyDoor door in outerDoors) {
        door.Enabled = true;
    }
}

void onTakeoff() {
    foreach(IMyLandingGear connector in landingGear) {
        if(connector.CustomData.Contains("WasLanded;")) connector.CustomData = connector.CustomData.Replace("WasLanded;", "");
    }

    foreach(IMyThrust thruster in allThrusters) {
        thruster.Enabled = true;
    }

    foreach(IMyDoor door in outerDoors) {
        door.CloseDoor();
    }

    outerDoorDisableTimer = 10;
}

void onDock() {
    foreach(IMyShipConnector connector in connectors) {
        if(!connector.CustomData.Contains("WasDocked;")) connector.CustomData = connector.CustomData + "WasDocked;";
    }

    foreach(IMyThrust thruster in allThrusters) {
        thruster.Enabled = false;
    }

    if(!amInSpace()) {
        foreach(IMyAirVent vent in airVents) {
            vent.Depressurize = true;
        }

        foreach(IMyGasTank tank in oxygenTanks) {
            tank.Stockpile = true;
        }
    }

    if(isHydrogenShip) {
        foreach(IMyGasTank tank in hydrogenTanks) {
            tank.Stockpile = true;
        }
    }

    bool debounce = true;
    foreach(IMyBatteryBlock battery in batteries) {
        if(debounce) debounce = false;
        else battery.ChargeMode = ChargeMode.Recharge;
    }

    foreach(IMyInteriorLight light in exteriorLights) {
        light.Enabled = false;
    }

    foreach(IMyDoor door in outerDoors) {
        door.Enabled = true;
    }
}

void onUndock() {
    foreach(IMyShipConnector connector in connectors) {
        if(connector.CustomData.Contains("WasDocked;")) connector.CustomData = connector.CustomData.Replace("WasDocked;", "");
    }

    foreach(IMyThrust thruster in allThrusters) {
        thruster.Enabled = true;
    }

    if(!amInSpace()) {
        foreach(IMyAirVent vent in airVents) {
            vent.Depressurize = false;
        }

        foreach(IMyGasTank tank in oxygenTanks) {
            tank.Stockpile = false;
        }
    }

    if(isHydrogenShip) {
        foreach(IMyGasTank tank in hydrogenTanks) {
            tank.Stockpile = false;
        }
    }

    foreach(IMyBatteryBlock battery in batteries) {
        battery.ChargeMode = ChargeMode.Auto;
    }

    foreach(IMyInteriorLight light in exteriorLights) {
        light.Enabled = true;
    }

    foreach(IMyDoor door in outerDoors) {
        door.CloseDoor();
    }

    outerDoorDisableTimer = 10;
}

string doStatusLights(int powerLevel, int oxygenLevel, int fuelLevel) {
    List<String> warningStatus = new List<String>();
    Color statusColor = Color.White;
    bool statusBlink = false;
    
    if(powerLevel < (isHydrogenShip ? 5 : 20)) {
        warningStatus.Add("Low Power");
        statusColor = lowPower;
        statusBlink = true;
    } else if(powerLevel < (isHydrogenShip ? 25 : 40)) {
        warningStatus.Add("Low Power");
        statusColor = lowPower;
    } 

    if(hasOxygenTanks) {
        if(oxygenLevel < 5) {
            warningStatus.Add("Low Oxygen");
            statusColor = lowOxygen;
            statusBlink = true;
        } else if(oxygenLevel < 25) {
            warningStatus.Add("Low Oxygen");
            statusColor = lowOxygen;
        } 
    }

    if(isHydrogenShip) {
        if(fuelLevel < 10) {
            warningStatus.Add("Low Fuel");
            statusColor = lowFuel;
            statusBlink = true;
        } else if(fuelLevel < 30) {
            warningStatus.Add("Low Fuel");
            statusColor = lowFuel;
        }
    }

    float totalMass = controller.CalculateShipMass().TotalMass;

    if(getThrusterLimitPercentage() > 95 && weightLimit == 0 || weightLimit != 0 && weightLimit * 0.95 < totalMass) {
        warningStatus.Add("Thruster limit");
        statusColor = depressurized;
        statusBlink = true;
    } else if(getThrusterLimitPercentage() > 85 && weightLimit == 0 || weightLimit != 0 && weightLimit * 0.85 < totalMass) {
        warningStatus.Add("Thruster limit");
        statusColor = lowFuel;
        statusBlink = true;
    }

    if(amInSpace() && notAirtight()) {
        warningStatus.Add("Depressurized");
        statusColor = depressurized;
        statusBlink = true;
    }
    
    foreach(IMyInteriorLight light in statusLights) {
        if(light.Color != statusColor) {
            light.Enabled = (statusColor == Color.White ? false : true);
            light.Color = statusColor;
        }

        if(statusBlink && light.BlinkLength == 0) {
            light.BlinkLength = 50F;
            light.BlinkIntervalSeconds = 2;
        } else if(!statusBlink && light.BlinkLength != 0) {
            light.BlinkIntervalSeconds = 0;
            light.BlinkLength = 0;
        }
    }

    return String.Join(" - ", warningStatus.ToArray());
}

bool amInSpace() {
    return (controller.GetNaturalGravity().Sum > -6);
}

bool notAirtight() {
    foreach(IMyAirVent item in airVents) {
        if(item.Enabled && !item.CanPressurize && item.PressurizationEnabled) return true;
    }

    return false;
}

void Log(string text) {
    IMyTextSurfaceProvider cockpitScreen = (IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName(shipName + "PilotSeat");
    cockpitScreen.GetSurface(0).WriteText(text, false);
    foreach(IMyTextPanel screen in statusScreens) {
        screen.WriteText(text, false);
    }
}

int getPowerAsInt(string text) {   
    if (String.IsNullOrWhiteSpace(text)) return 0;

    string[] values = text.Split(' ');
    
    if (values[1].Equals("kW")) return (int) (float.Parse(values[0])*1000f);
    else if (values[1].Equals("kWh")) return (int) (float.Parse(values[0])*1000f); 
    else if (values[1].Equals("MW")) return (int) (float.Parse(values[0])*1000000f);
    else if (values[1].Equals("MWh")) return (int) (float.Parse(values[0])*1000000f); 
    else return (int) float.Parse(values[0]);
    
    return 0;
}

int getPower(IMyTerminalBlock block, bool max) {   
    if (max && !block.IsBeingHacked) return getPowerAsInt(getDetailedInfoValue(block, "Max Stored Power"));
    else return getPowerAsInt(getDetailedInfoValue(block, "Stored power"));
    return 0;
}

string getDetailedInfoValue(IMyTerminalBlock block, string name) {
    string value = "";
    string[] lines = block.DetailedInfo.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    
    for (int i = 0; i < lines.Length; i = i + 1) {
        string[] line = lines[i].Split(':');
        if (line[0].Equals(name)) {
            value = line[1].Substring(1);
            break;
        }
    }

    return value;
}

string getFillLevel(int amt) {
    string returnValue = "";

    for(int i = 0; i < 20; i = i + 1) {
        returnValue = returnValue + (i*5 < amt ? "|" : "'");
    }

    return returnValue;
}

/*

Features:
Marca niveles de:
Energia
Combustible (Si aplica)
Oxigeno (Si aplica)
Porcentaje de uso de thrusters (Masa * gravedad / el minimo de thrust en newtons)

Al dockear:
Apaga motores
Pone baterias para recargar
Pone los tanques a recargar (Oxigeno solo si esta en una atmosfera con oxigeno)
Destraba las puertas exteriores (no las abre)
Apaga luces exteriores de posicion

Al undockear:
Prende motores
Pone baterias en Auto
Pone tanques a aportar gas en vez de almacenar
Cierra y traba las puertas exteriores
Prende luces exteriores de posicion
*/