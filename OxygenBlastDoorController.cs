static String gridId;
const int VERSION = 1;

class ColorState {
    public Color color;
    public float intensity;
}
class BlastSection { 
    public string sectorId = "";
    public bool isLocked = false;
    public int doorDebounce = 0;
    public List<IMyDoor> blastDoors = new List<IMyDoor>();
    public List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
    public List<ColorState> originalLightColors = new List<ColorState>();
    public IMyAirVent airVent;
}

List<IMyBlockGroup> sectorGroups = new List<IMyBlockGroup>();
List<BlastSection> sections = new List<BlastSection>();

int tick = 0;

string errors = "";

public Program()
{
    gridId = Me.CubeGrid.ToString();

    GridTerminalSystem.GetBlockGroups(sectorGroups, group => group.Name.Contains("Section_"));

    if (!sectorGroups.Any())
    {
        Log("No section groups defined!");
        return;
    }

    foreach(IMyBlockGroup sectorGroup in sectorGroups) {
        string sectorErrors = "";
        BlastSection sector = new BlastSection();

        string sectorId = sectorGroup.Name.Replace("Section_", "");
        sector.sectorId = sectorId;

        sectorGroup.GetBlocksOfType(sector.blastDoors, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("BlastDoor"));
        sectorGroup.GetBlocksOfType(sector.lights, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("Light"));

        List<IMyAirVent> airVents = new List<IMyAirVent>();
        sectorGroup.GetBlocksOfType(airVents, block => block.IsFunctional && block.CubeGrid.ToString() == gridId && block.CustomName.Contains("Vent"));

        if(!airVents.Any()) {
            sectorErrors += "\n\tNo airvents in section!";
        } else {
            IMyAirVent airVent = airVents.ElementAt(0);
            sector.airVent = airVent;
            sector.isLocked = airVent.GetOxygenLevel() <= 0.5;
        }

        if(!sector.blastDoors.Any()) {
            sectorErrors += "\n\tNo blast doors in section!";
        } else {
            if(sector.isLocked) {
                foreach(IMyDoor door in sector.blastDoors) {
                    door.CloseDoor();
                    sector.doorDebounce = 5;
                }
            } else {
                foreach(IMyDoor door in sector.blastDoors) {
                    door.Enabled = true;
                }
            }
        }

        if(!sector.lights.Any()) {
            sectorErrors += "\n\tNo lights in section!";
        } else {
            foreach(IMyInteriorLight light in sector.lights) {
                ColorState originalColorState = new ColorState();
                originalColorState.intensity = light.Intensity;
                originalColorState.color = light.Color;

                sector.originalLightColors.Add(originalColorState);

                if(sector.isLocked) {
                    light.Intensity = 2;
                    light.Color = Color.Red; 
                }
            }
        }

        if(sectorErrors != "") {
            errors += "\n\tSection " + sectorId;
            errors += sectorErrors + "\n\t";
        }

        sections.Add(sector);
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource)
{
    if(errors != "") return;

    tick = tick + 1;
    if (tick > 3) tick = 0;

    string sectionsInLockdown = "";

    foreach(BlastSection section in sections) {
        if(section.doorDebounce > 0) {
            section.doorDebounce = section.doorDebounce - 1;

            if(section.doorDebounce == 0) {
                foreach(IMyDoor door in section.blastDoors) {
                    door.Enabled = !section.isLocked;
                }
            }
        }

        if(section.airVent.GetOxygenLevel() > 0.5 && section.isLocked) {
            section.isLocked = false;

            for(int i = 0; i < section.lights.Count; i++) {
                IMyInteriorLight light = section.lights.ElementAt(i);
                ColorState originalState = section.originalLightColors.ElementAt(i);

                light.Intensity = originalState.intensity;
                light.Color = originalState.color;
            }
        } else if(section.airVent.GetOxygenLevel() < 0.5 && !section.isLocked) {
            section.isLocked = true;
            section.doorDebounce = 5;

            for(int i = 0; i < section.lights.Count; i++) {
                IMyInteriorLight light = section.lights.ElementAt(i);

                light.Intensity = 2;
                light.Color = Color.Red;
            }

            foreach(IMyDoor door in section.blastDoors) {
                door.CloseDoor();
                door.Enabled = true;
            }
        }

        if(section.isLocked) {
            sectionsInLockdown += "\n\tSection " + section.sectorId;
        }
    }

    String textToShow = "Version " + VERSION + " of Blast Door Program - " + getWorkingAnimation();
    textToShow = textToShow + "\n\tSections - " + sections.Count;

    if(sectionsInLockdown != "") textToShow = textToShow + "\n\t\n\tSections in lockdown:" + sectionsInLockdown;

    Log(textToShow);
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
