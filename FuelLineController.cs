// Use FuelLine blueprint
////// de aca para abajo no tocar \\\\\\

const int VERSION = 1;

IMyShipController controller;
IMyPistonBase pistonUD;
IMyPistonBase pistonLR;
IMyPistonBase pistonFB;
string structureName;

public Program() {
    structureName = Me.CustomName.Replace("_PB", "");
    List<IMyTerminalBlock> allFuelLineBlocks = new List<IMyTerminalBlock>();
    IMyBlockGroup fuelLineBlockGroup = GridTerminalSystem.GetBlockGroupWithName(structureName);
    if(fuelLineBlockGroup == null) return;

    fuelLineBlockGroup.GetBlocks(allFuelLineBlocks, block => block.IsFunctional);

    foreach(IMyTerminalBlock block in allFuelLineBlocks) {
        if(block.CustomName.Contains(structureName + "_Lower")) pistonUD = (IMyPistonBase)block;
        else if(block.CustomName.Contains(structureName + "_Extend")) pistonLR = (IMyPistonBase)block;
        else if(block.CustomName.Contains(structureName + "_FB")) pistonFB = (IMyPistonBase)block;
        else if(block.CustomName.Contains(structureName + "_Controller")) controller = (IMyShipController)block;
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource) {
    IMyTextSurfaceProvider controllerScreen = (IMyTextSurfaceProvider)controller;
    controllerScreen.GetSurface(0).WriteText(controller.MoveIndicator.ToString());

    switch((int)controller.MoveIndicator.X) {
        case -1: pistonLR.Velocity = 1F; break;
        case 1: pistonLR.Velocity = -1F; break;
        default: pistonLR.Velocity = 0; break;
    }
    
    switch((int)controller.MoveIndicator.Y) {
        case -1: pistonUD.Velocity = 1F; break;
        case 1: pistonUD.Velocity = -1F; break;
        default: pistonUD.Velocity = 0; break;
    }

    switch((int)controller.MoveIndicator.Z) {
        case 1: pistonFB.Velocity = 1F; break;
        case -1: pistonFB.Velocity = -1F; break;
        default: pistonFB.Velocity = 0; break;
    }
}
