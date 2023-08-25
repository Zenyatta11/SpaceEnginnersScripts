const string craneName = "Crane0";

////// de aca para abajo no tocar \\\\\\

const int VERSION = 1;

List<IMyReflectorLight> warningLights = new List<IMyReflectorLight>();
IMyShipController controller;
IMyPistonBase pistonUD;
IMyPistonBase pistonLR;
IMyPistonBase pistonFB;

bool isUnderControl = false;

public Program() {
    List<IMyTerminalBlock> allCraneBlocks = new List<IMyTerminalBlock>();
    IMyBlockGroup craneBlockGroup = GridTerminalSystem.GetBlockGroupWithName(craneName);
    if(craneBlockGroup == null) return;

    craneBlockGroup.GetBlocks(allCraneBlocks, block => block.IsFunctional);
    craneBlockGroup.GetBlocksOfType(warningLights, block => block.IsFunctional);

    foreach(IMyTerminalBlock block in allCraneBlocks) {
        if(block.CustomName.Contains("Piston_UD")) pistonUD = (IMyPistonBase)block;
        else if(block.CustomName.Contains("Piston_FB")) pistonFB = (IMyPistonBase)block;
        else if(block.CustomName.Contains("Piston_LR")) pistonLR = (IMyPistonBase)block;
        else if(block.CustomName.Contains("Controller")) controller = (IMyShipController)block;
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource) {
    if(controller.IsUnderControl && !isUnderControl) {
        toggleLights(true);
        isUnderControl = true;
    } else if(!controller.IsUnderControl && isUnderControl) {
        toggleLights(false);
        isUnderControl = false;
    }

    IMyTextSurfaceProvider controllerScreen = (IMyTextSurfaceProvider)controller;
    controllerScreen.GetSurface(0).WriteText(controller.MoveIndicator.ToString());

    switch((int)controller.MoveIndicator.X) {
        case -1: pistonLR.Velocity = 1F; break;
        case 1: pistonLR.Velocity = -1F; break;
        default: pistonLR.Velocity = 0; break;
    }

    switch((int)controller.MoveIndicator.Y) {
        case 1: pistonUD.Velocity = 1F; break;
        case -1: pistonUD.Velocity = -1F; break;
        default: pistonUD.Velocity = 0; break;
    }

    switch((int)controller.MoveIndicator.Z) {
        case -1: pistonFB.Velocity = 1F; break;
        case 1: pistonFB.Velocity = -1F; break;
        default: pistonFB.Velocity = 0; break;
    }
}

void toggleLights(bool enable) {
    foreach(IMyReflectorLight light in warningLights) {
        light.Enabled = enable;
    }
}