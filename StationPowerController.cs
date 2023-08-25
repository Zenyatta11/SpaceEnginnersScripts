const String stationName = "Base Terrestre";

////// de aca para abajo no tocar \\\\\\

static String gridId;
const int VERSION = 1;

List<IMyTextPanel> statusScreens = new List<IMyTextPanel>();
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
List<IMyGasTank> hydrogenTanks = new List<IMyGasTank>();
List<IMyFunctionalBlock> hydrogenGenerators = new List<IMyFunctionalBlock>();

int tick = 0;
bool hasOxygenTanks = false;
bool hasHydrogenTanks = false;
bool hasHydrogenGenerators = false;
bool generatorsRunning = false;

public Program() {
    gridId = Me.CubeGrid.ToString();

    IMyBlockGroup hydrogenGeneratorGroup = GridTerminalSystem.GetBlockGroupWithName("BaseGenerators");
    if(hydrogenGeneratorGroup != null) {
        hasHydrogenGenerators = true;
        hydrogenGeneratorGroup.GetBlocksOfType(hydrogenGenerators, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    }

    GridTerminalSystem.GetBlocksOfType(statusScreens, block => block.IsFunctional && block.CustomName.Contains("Status") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsFunctional && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(oxygenTanks, block => block.IsFunctional && block.CustomName.Contains("Oxygen") && block.CubeGrid.ToString() == gridId);
    GridTerminalSystem.GetBlocksOfType(hydrogenTanks, block => block.IsFunctional && block.CustomName.Contains("Hydrogen") && block.CubeGrid.ToString() == gridId);

    hasOxygenTanks = oxygenTanks.Any();
    hasHydrogenTanks = hydrogenTanks.Any();

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource) {
    tick = tick + 1;
	if(tick > 3) tick = 0;

    int powerLevel = (int)getPowerLevel();
    int oxygenLevel = (int)getGasLevel(oxygenTanks);
    int fuelLevel = (int)getGasLevel(hydrogenTanks);

    if(powerLevel < 50 && fuelLevel > 50 && !generatorsRunning) toggleGenerators(true);
    else if((powerLevel > 80 || fuelLevel < 20) && generatorsRunning) toggleGenerators(false);

    String textToShow = "Version " + VERSION + " of Program - " + getWorkingAnimation();
    textToShow = textToShow + "\n\t[" + getFillLevel(powerLevel) + "] " + String.Format("{0,4}", powerLevel) + "% | " + String.Format("{0,8}", "Power");

    if(hasOxygenTanks) textToShow = textToShow + "\n\t[" + getFillLevel(oxygenLevel) + "] " +  String.Format("{0,4}", oxygenLevel) + "% | " + String.Format("{0,8}", "Oxygen");
    if(hasHydrogenTanks) textToShow = textToShow + "\n\t[" + getFillLevel(fuelLevel) + "] " +  String.Format("{0,4}", fuelLevel) + "% | " + String.Format("{0,8}", "Fuel");
    if(hasHydrogenGenerators) textToShow = textToShow + "\n\tGenerators " + (generatorsRunning ? "Online" : "Offline");
    textToShow = textToShow + "\n\tNet charge: " + batteries[0].CurrentInput;

    Log(textToShow);
}

int getNetCharge() {
    return 0;
}

String parseUnit(int numeric, String unit) {
    return "";
}

void toggleGenerators(bool enable) {
    generatorsRunning = enable;
    foreach(IMyFunctionalBlock item in hydrogenGenerators) {
        item.Enabled = enable;
    }
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

string getWorkingAnimation() {
	switch(tick) {
		case 0: return "|";
		case 1: return "/";
		case 2: return "-";
		case 3: return "\\";
		default: return "x";
	}
}

void Log(string text) {
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
